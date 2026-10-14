using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Departments;
using BusinessOperations.Application.DTOs.Employees;
using BusinessOperations.Application.DTOs.Expenses;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests.Controllers;

public class ExpensesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExpensesEndpointTests(WebApplicationFactory<Program> factory)
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

        string email = $"expuser_{Guid.NewGuid():N}@test.com";
        var user = new User(email, hasher.HashPassword("P@ssword123!"), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, _) = jwtGen.GenerateToken(user);

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateAdminToken());

        var deptResponse = await adminClient.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = $"ExpDept_{Guid.NewGuid():N}",
            Description = "Department for expense testing"
        });
        deptResponse.EnsureSuccessStatusCode();
        var deptDto = await deptResponse.Content.ReadFromJsonAsync<DepartmentDto>();

        var empResponse = await adminClient.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Expense",
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
    public async Task Submit_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 50.00m,
            Category = ExpenseCategory.Meals,
            Description = "Lunch with client",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithValidData_ReturnsCreated_AndExpense()
    {
        var (_, empId, _, client) = await CreateEmployeeWithUserAsync();

        var response = await client.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 125.50m,
            Currency = "USD",
            Category = ExpenseCategory.Travel,
            Description = "Taxi to conference center",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(125.50m, created.Amount);
        Assert.Equal("USD", created.Currency);
        Assert.Equal(ExpenseCategory.Travel, created.Category);
        Assert.Equal(ExpenseStatus.Pending, created.Status);
        Assert.Equal(empId, created.EmployeeId);
    }

    [Fact]
    public async Task Submit_WithNegativeAmount_ReturnsBadRequest()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync();

        var response = await client.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = -10.00m,
            Category = ExpenseCategory.OfficeSupplies,
            Description = "Invalid negative amount",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Submit_WithFutureDate_ReturnsBadRequest()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync();

        var response = await client.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 45.00m,
            Category = ExpenseCategory.Meals,
            Description = "Future dinner",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) // Future date
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyExpenses_ReturnsCallerExpenses()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync();

        await client.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 30.00m,
            Category = ExpenseCategory.Equipment,
            Description = "Keyboard and mouse",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        var response = await client.GetAsync("/api/expenses/my");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<IReadOnlyList<ExpenseDto>>();
        Assert.NotNull(list);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task GetPaged_WithEmployeeToken_ReturnsForbidden()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync(UserRole.Employee);
        var response = await client.GetAsync("/api/expenses");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithManagerToken_ReturnsOk()
    {
        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);
        var response = await managerClient.GetAsync("/api/expenses?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResult<ExpenseDto>>();
        Assert.NotNull(paged);
    }

    [Fact]
    public async Task Approve_ByOtherManager_SetsApproved()
    {
        var (_, _, _, empClient) = await CreateEmployeeWithUserAsync(UserRole.Employee);

        var createResponse = await empClient.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 88.00m,
            Category = ExpenseCategory.Meals,
            Description = "Dinner meeting",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseDto>();

        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);

        var approveResponse = await managerClient.PostAsJsonAsync($"/api/expenses/{created!.Id}/approve", new ReviewExpenseDto
        {
            Notes = "Receipt verified and approved."
        });

        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);
        var approved = await approveResponse.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(approved);
        Assert.Equal(ExpenseStatus.Approved, approved.Status);
        Assert.Equal("Receipt verified and approved.", approved.ReviewNotes);
    }

    [Fact]
    public async Task Approve_SelfApprovalAttempt_ReturnsBadRequest()
    {
        // Manager submits their own expense
        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);

        var createResponse = await managerClient.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 150.00m,
            Category = ExpenseCategory.Travel,
            Description = "Flight ticket",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseDto>();

        // Manager attempts self-approval
        var approveResponse = await managerClient.PostAsJsonAsync($"/api/expenses/{created!.Id}/approve", new ReviewExpenseDto
        {
            Notes = "Self-approving my ticket"
        });

        Assert.Equal(HttpStatusCode.BadRequest, approveResponse.StatusCode);
    }

    [Fact]
    public async Task Reject_ByManager_RequiresReason_AndSetsRejected()
    {
        var (_, _, _, empClient) = await CreateEmployeeWithUserAsync(UserRole.Employee);

        var createResponse = await empClient.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 250.00m,
            Category = ExpenseCategory.Other,
            Description = "Unspecified expense",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseDto>();

        var (_, _, _, managerClient) = await CreateEmployeeWithUserAsync(UserRole.Manager);

        // Reject with empty reason fails
        var emptyRejectResponse = await managerClient.PostAsJsonAsync($"/api/expenses/{created!.Id}/reject", new ReviewExpenseDto
        {
            Notes = ""
        });
        Assert.Equal(HttpStatusCode.BadRequest, emptyRejectResponse.StatusCode);

        // Reject with whitespace-only RejectionReason property fails
        var whitespaceRejectResponse = await managerClient.PostAsJsonAsync($"/api/expenses/{created.Id}/reject", new ReviewExpenseDto
        {
            RejectionReason = "   "
        });
        Assert.Equal(HttpStatusCode.BadRequest, whitespaceRejectResponse.StatusCode);

        // Reject with valid RejectionReason property succeeds
        var rejectResponse = await managerClient.PostAsJsonAsync($"/api/expenses/{created.Id}/reject", new ReviewExpenseDto
        {
            RejectionReason = "Missing itemized tax invoice."
        });
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(rejected);
        Assert.Equal(ExpenseStatus.Rejected, rejected.Status);
        Assert.Equal("Missing itemized tax invoice.", rejected.ReviewNotes);
    }

    [Fact]
    public async Task Cancel_PendingExpenseByOwner_SetsCancelled()
    {
        var (_, _, _, empClient) = await CreateEmployeeWithUserAsync(UserRole.Employee);

        var createResponse = await empClient.PostAsJsonAsync("/api/expenses", new CreateExpenseDto
        {
            Amount = 15.00m,
            Category = ExpenseCategory.OfficeSupplies,
            Description = "Notebooks - will cancel",
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseDto>();

        var cancelResponse = await empClient.PostAsync($"/api/expenses/{created!.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<ExpenseDto>();
        Assert.NotNull(cancelled);
        Assert.Equal(ExpenseStatus.Cancelled, cancelled.Status);
    }
}
