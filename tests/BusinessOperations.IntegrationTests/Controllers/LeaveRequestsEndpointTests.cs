using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Auth;
using BusinessOperations.Application.DTOs.Departments;
using BusinessOperations.Application.DTOs.Employees;
using BusinessOperations.Application.DTOs.LeaveRequests;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests.Controllers;

public class LeaveRequestsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LeaveRequestsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private string GenerateToken(UserRole role, Guid? userId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var user = new User($"user_{Guid.NewGuid():N}@test.com", "hash", role);
        if (userId.HasValue)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(user, userId.Value);
        }
        var (token, _) = jwtGenerator.GenerateToken(user);
        return token;
    }

    private async Task<Guid> CreateDepartmentAsync(HttpClient adminClient)
    {
        var response = await adminClient.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = $"LeaveDept_{Guid.NewGuid():N}",
            Description = "Department for leave testing"
        });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DepartmentDto>();
        return dto!.Id;
    }

    private async Task<(Guid UserId, Guid EmployeeId, string Token, HttpClient Client)> CreateEmployeeWithUserAsync(
        UserRole role = UserRole.Employee,
        int leaveBalance = 20)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtGen = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        string email = $"emp_{Guid.NewGuid():N}@test.com";
        var user = new User(email, hasher.HashPassword("P@ssword123!"), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, _) = jwtGen.GenerateToken(user);

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var deptId = await CreateDepartmentAsync(adminClient);

        var empResponse = await adminClient.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Leave",
            LastName = "Tester",
            Email = email,
            DepartmentId = deptId,
            HireDate = new DateOnly(2025, 1, 1),
            AnnualLeaveBalanceDays = leaveBalance,
            UserId = user.Id
        });
        empResponse.EnsureSuccessStatusCode();
        var empDto = await empResponse.Content.ReadFromJsonAsync<EmployeeDto>();

        var userClient = _factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (user.Id, empDto!.Id, token, userClient);
    }

    [Fact]
    public async Task Submit_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            Reason = "Vacation"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithValidData_ReturnsCreated_AndLeaveRequest()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee, leaveBalance: 20);

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)); // 5 days inclusive

        var response = await client.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Family trip"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<LeaveRequestDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(5, created.DaysCount);
        Assert.Equal(LeaveStatus.Pending, created.Status);
        Assert.Equal("Family trip", created.Reason);
    }

    [Fact]
    public async Task Submit_WithInsufficientBalance_ReturnsBadRequest()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee, leaveBalance: 2);

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)); // 6 days, exceeds balance of 2

        var response = await client.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Reason = "Extended holiday"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithOverlappingDates_ReturnsBadRequest()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee, leaveBalance: 20);

        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25));

        var first = await client.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Reason = "First leave"
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Overlapping submission (days 22 to 24)
        var second = await client.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(22)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(24)),
            Reason = "Overlapping leave attempt"
        });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task GetMyRequests_ReturnsEmployeesOwnRequests()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee, leaveBalance: 20);

        await client.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(31)),
            Reason = "Doctor appointment"
        });

        var response = await client.GetAsync("/api/leaverequests/my");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<LeaveRequestDto>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetPaged_WithEmployeeToken_ReturnsForbidden()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee);
        var response = await client.GetAsync("/api/leaverequests");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithManagerToken_ReturnsOk()
    {
        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);
        var response = await managerClient.GetAsync("/api/leaverequests?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<LeaveRequestDto>>();
        Assert.NotNull(paged);
    }

    [Fact]
    public async Task Approve_ByOtherManager_DeductsLeaveBalance_AndSetsApproved()
    {
        // Submitting employee has 20 balance
        var (empUserId, empId, _, empClient) = await CreateEmployeeWithUserAsync(UserRole.Employee, leaveBalance: 20);

        var submitResponse = await empClient.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(42)), // 3 days inclusive
            Reason = "Personal time"
        });
        submitResponse.EnsureSuccessStatusCode();
        var submitted = await submitResponse.Content.ReadFromJsonAsync<LeaveRequestDto>();

        // Distinct Manager approves
        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);
        var approveResponse = await managerClient.PostAsJsonAsync($"/api/leaverequests/{submitted!.Id}/approve", new ReviewLeaveRequestDto
        {
            Notes = "Approved, enjoy your time off."
        });

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approved = await approveResponse.Content.ReadFromJsonAsync<LeaveRequestDto>();
        Assert.NotNull(approved);
        Assert.Equal(LeaveStatus.Approved, approved.Status);
        Assert.Equal("Approved, enjoy your time off.", approved.ReviewNotes);

        // Verify balance was deducted (20 - 3 = 17)
        var empProfileResponse = await empClient.GetAsync($"/api/employees/{empId}");
        empProfileResponse.EnsureSuccessStatusCode();
        var empProfile = await empProfileResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.Equal(17, empProfile!.AnnualLeaveBalanceDays);
    }

    [Fact]
    public async Task Approve_SelfApprovalAttempt_ReturnsBadRequest()
    {
        // Manager creates their own leave request
        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager, leaveBalance: 20);

        var submitResponse = await managerClient.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(50)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(51)),
            Reason = "Manager holiday"
        });
        submitResponse.EnsureSuccessStatusCode();
        var submitted = await submitResponse.Content.ReadFromJsonAsync<LeaveRequestDto>();

        // Manager attempts to approve their OWN request
        var approveResponse = await managerClient.PostAsJsonAsync($"/api/leaverequests/{submitted!.Id}/approve", new ReviewLeaveRequestDto
        {
            Notes = "Self approval attempt"
        });

        Assert.Equal(HttpStatusCode.BadRequest, approveResponse.StatusCode);
    }

    [Fact]
    public async Task Reject_ByManager_SetsRejected_AndRequiresReason()
    {
        var (_, _, _, empClient) = await CreateEmployeeWithUserAsync(UserRole.Employee, leaveBalance: 20);

        var submitResponse = await empClient.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(62)),
            Reason = "Conference"
        });
        submitResponse.EnsureSuccessStatusCode();
        var submitted = await submitResponse.Content.ReadFromJsonAsync<LeaveRequestDto>();

        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);

        // Reject with empty reason fails
        var emptyRejectResponse = await managerClient.PostAsJsonAsync($"/api/leaverequests/{submitted!.Id}/reject", new ReviewLeaveRequestDto
        {
            Notes = ""
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyRejectResponse.StatusCode);

        // Reject with whitespace-only RejectionReason property fails
        var whitespaceRejectResponse = await managerClient.PostAsJsonAsync($"/api/leaverequests/{submitted.Id}/reject", new ReviewLeaveRequestDto
        {
            RejectionReason = "   "
        });
        Assert.Equal(HttpStatusCode.BadRequest, whitespaceRejectResponse.StatusCode);

        // Reject with valid RejectionReason property succeeds
        var rejectResponse = await managerClient.PostAsJsonAsync($"/api/leaverequests/{submitted.Id}/reject", new ReviewLeaveRequestDto
        {
            RejectionReason = "Critical release deadline overlap."
        });
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<LeaveRequestDto>();
        Assert.NotNull(rejected);
        Assert.Equal(LeaveStatus.Rejected, rejected.Status);
        Assert.Equal("Critical release deadline overlap.", rejected.ReviewNotes);
    }

    [Fact]
    public async Task Cancel_PendingRequestByOwner_SetsCancelled()
    {
        var (_, _, _, empClient) = await CreateEmployeeWithUserAsync(UserRole.Employee, leaveBalance: 20);

        var submitResponse = await empClient.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(70)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(71)),
            Reason = "To be cancelled"
        });
        submitResponse.EnsureSuccessStatusCode();
        var submitted = await submitResponse.Content.ReadFromJsonAsync<LeaveRequestDto>();

        var cancelResponse = await empClient.PostAsync($"/api/leaverequests/{submitted!.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<LeaveRequestDto>();
        Assert.NotNull(cancelled);
        Assert.Equal(LeaveStatus.Cancelled, cancelled.Status);
    }
}
