using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessOperations.Application.DTOs.Dashboard;
using BusinessOperations.Application.DTOs.Departments;
using BusinessOperations.Application.DTOs.Employees;
using BusinessOperations.Application.DTOs.Expenses;
using BusinessOperations.Application.DTOs.LeaveRequests;
using BusinessOperations.Application.DTOs.Tasks;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests.Controllers;

public class DashboardEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private string GenerateAdminToken()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var user = new User($"admin_{Guid.NewGuid():N}@test.com", "hash", UserRole.Admin);
        var (token, _) = jwtGenerator.GenerateToken(user);
        return token;
    }

    private async Task<(Guid UserId, Guid EmployeeId, string Token, HttpClient Client)> CreateEmployeeWithUserAsync(
        UserRole role = UserRole.Employee)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtGen = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        string email = $"dashuser_{Guid.NewGuid():N}@test.com";
        var user = new User(email, hasher.HashPassword("P@ssword123!"), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, _) = jwtGen.GenerateToken(user);

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateAdminToken());

        var deptResponse = await adminClient.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = $"DashDept_{Guid.NewGuid():N}",
            Description = "Department for dashboard testing"
        });
        deptResponse.EnsureSuccessStatusCode();
        var deptDto = await deptResponse.Content.ReadFromJsonAsync<DepartmentDto>();

        var empResponse = await adminClient.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Dashboard",
            LastName = "Employee",
            Email = email,
            DepartmentId = deptDto!.Id,
            HireDate = new DateOnly(2025, 1, 1),
            UserId = user.Id
        });
        empResponse.EnsureSuccessStatusCode();
        var empDto = await empResponse.Content.ReadFromJsonAsync<EmployeeDto>();

        var userClient = _factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (user.Id, empDto!.Id, token, userClient);
    }

    [Fact]
    public async Task GetSummary_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithEmployeeToken_ReturnsForbidden()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee);
        var response = await client.GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSummary_WithManagerToken_ReturnsOk_WithAggregates()
    {
        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);

        var response = await managerClient.GetAsync("/api/dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.TotalEmployees >= 1);
        Assert.True(summary.TotalDepartments >= 1);
        Assert.True(summary.TotalTasks >= 0);
        Assert.True(summary.PendingLeaveRequests >= 0);
        Assert.True(summary.PendingExpensesCount >= 0);
    }

    [Fact]
    public async Task GetPersonalSummary_WithEmployeeToken_ReturnsAccuratePersonalCounts()
    {
        var (_, empId, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee);

        // 1. Submit leave request
        await client.PostAsJsonAsync("/api/leaverequests", new CreateLeaveRequestDto
        {
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            Reason = "Trip"
        });

        // 2. Submit expense
        await client.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 45.00m,
            Category = ExpenseCategory.Meals,
            Description = "Business meal",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        // 3. Create and assign task to this employee
        await client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Personal Task",
            AssignedToEmployeeId = empId
        });

        // Query personal dashboard
        var response = await client.GetAsync("/api/dashboard/my");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var personal = await response.Content.ReadFromJsonAsync<PersonalDashboardDto>();
        Assert.NotNull(personal);
        Assert.Equal(empId, personal.EmployeeId);
        Assert.Equal("Dashboard Employee", personal.FullName);
        Assert.True(personal.PendingLeaveRequests >= 1);
        Assert.True(personal.PendingExpensesCount >= 1);
        Assert.True(personal.TotalPendingExpensesAmount >= 45.00m);
        Assert.True(personal.AssignedTasksCount >= 1);
    }
}
