using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Auth;
using BusinessOperations.Application.DTOs.Departments;
using BusinessOperations.Application.DTOs.Employees;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests.Controllers;

public class EmployeesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EmployeesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private string GenerateToken(UserRole role, Guid? userId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var jwtGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var user = new User($"test_{Guid.NewGuid():N}@test.com", "dummyhash", role);
        if (userId.HasValue)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(user, userId.Value);
        }
        var (token, _) = jwtGenerator.GenerateToken(user);
        return token;
    }

    private async Task<(Guid UserId, string Token)> RegisterUserAsync(string email)
    {
        var client = _factory.CreateClient();
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "P@ssword123!",
            FirstName = "Test",
            LastName = "User"
        });
        regResponse.EnsureSuccessStatusCode();
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        return (auth!.UserId, auth.Token);
    }

    private async Task<Guid> CreateDepartmentAsync(HttpClient adminClient, string? namePrefix = null)
    {
        string deptName = $"{(namePrefix ?? "Dept")}_{Guid.NewGuid():N}";
        var response = await adminClient.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = deptName,
            Description = "Test department"
        });
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<DepartmentDto>();
        return dto!.Id;
    }

    [Fact]
    public async Task GetPaged_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithEmployeeToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Employee));

        var response = await client.GetAsync("/api/employees");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithManagerOrAdminToken_ReturnsOk_WithPagedResult()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Manager));

        var response = await client.GetAsync("/api/employees?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeDto>>();
        Assert.NotNull(result);
        Assert.True(result.PageNumber >= 1);
        Assert.True(result.PageSize >= 1);
    }

    [Fact]
    public async Task Create_WithNonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Manager));

        var response = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = $"johndoe_{Guid.NewGuid():N}@test.com",
            DepartmentId = Guid.NewGuid(),
            HireDate = new DateOnly(2025, 1, 1)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAdmin_ValidData_ReturnsCreated_AndEmployee()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var deptId = await CreateDepartmentAsync(client);
        string email = $"emp_{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Alice",
            LastName = "Wonderland",
            Email = email,
            DepartmentId = deptId,
            HireDate = new DateOnly(2025, 2, 1),
            AnnualLeaveBalanceDays = 25
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Alice", created.FirstName);
        Assert.Equal("Wonderland", created.LastName);
        Assert.Equal("Alice Wonderland", created.FullName);
        Assert.Equal(email, created.Email);
        Assert.Equal(deptId, created.DepartmentId);
        Assert.Equal(25, created.AnnualLeaveBalanceDays);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var deptId = await CreateDepartmentAsync(client);
        string email = $"dupe_emp_{Guid.NewGuid():N}@test.com";

        var payload = new CreateEmployeeDto
        {
            FirstName = "Bob",
            LastName = "Builder",
            Email = email,
            DepartmentId = deptId,
            HireDate = new DateOnly(2025, 3, 1)
        };

        var first = await client.PostAsJsonAsync("/api/employees", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/employees", payload);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenEmployeeViewsOtherProfile_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var (otherUserId, _) = await RegisterUserAsync($"otheruser_{Guid.NewGuid():N}@test.com");
        var (_, callerToken) = await RegisterUserAsync($"calleruser_{Guid.NewGuid():N}@test.com");

        var deptId = await CreateDepartmentAsync(client);
        var createResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Other",
            LastName = "User",
            Email = $"other_{Guid.NewGuid():N}@test.com",
            DepartmentId = deptId,
            HireDate = new DateOnly(2025, 1, 1),
            UserId = otherUserId
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(created);

        // Regular employee with a different UserId calls GetById
        var employeeClient = _factory.CreateClient();
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", callerToken);

        var response = await employeeClient.GetAsync($"/api/employees/{created.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenEmployeeViewsOwnProfile_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var (myUserId, myToken) = await RegisterUserAsync($"selfuser_{Guid.NewGuid():N}@test.com");
        var deptId = await CreateDepartmentAsync(client);
        var createResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Self",
            LastName = "Employee",
            Email = $"self_{Guid.NewGuid():N}@test.com",
            DepartmentId = deptId,
            HireDate = new DateOnly(2025, 1, 1),
            UserId = myUserId
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(created);

        // Employee calls with their own matching userId in claims
        var employeeClient = _factory.CreateClient();
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", myToken);

        var response = await employeeClient.GetAsync($"/api/employees/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(dto);
        Assert.Equal(created.Id, dto.Id);
    }

    [Fact]
    public async Task Update_EmployeeUpdatesOwnProfile_CanChangeName_CannotTamperDepartment()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var (myUserId, myToken) = await RegisterUserAsync($"updateuser_{Guid.NewGuid():N}@test.com");
        var originalDeptId = await CreateDepartmentAsync(client, "OriginalDept");
        var bogusDeptId = await CreateDepartmentAsync(client, "BogusDept");

        var createResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Updatable",
            LastName = "User",
            Email = $"updatable_{Guid.NewGuid():N}@test.com",
            DepartmentId = originalDeptId,
            HireDate = new DateOnly(2025, 1, 1),
            UserId = myUserId,
            AnnualLeaveBalanceDays = 20
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(created);

        // Employee tries to update their own profile, also attempting to switch department and increase leave balance
        var employeeClient = _factory.CreateClient();
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", myToken);

        var updateResponse = await employeeClient.PutAsJsonAsync($"/api/employees/{created.Id}", new UpdateEmployeeDto
        {
            FirstName = "UpdatedName",
            LastName = "UpdatedLastName",
            Email = created.Email,
            DepartmentId = bogusDeptId, // Attempted tampering
            AnnualLeaveBalanceDays = 999 // Attempted tampering
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(updated);
        Assert.Equal("UpdatedName", updated.FirstName);
        Assert.Equal("UpdatedLastName", updated.LastName);
        // Department and leave balance should NOT have changed because non-admin cannot tamper with them
        Assert.Equal(originalDeptId, updated.DepartmentId);
        Assert.Equal(20, updated.AnnualLeaveBalanceDays);
    }

    [Fact]
    public async Task Update_EmployeeCannotUpdateOtherProfile_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        var (otherUserId, _) = await RegisterUserAsync($"otherupdate_{Guid.NewGuid():N}@test.com");
        var (_, callerToken) = await RegisterUserAsync($"callerupdate_{Guid.NewGuid():N}@test.com");
        var deptId = await CreateDepartmentAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Target",
            LastName = "User",
            Email = $"target_{Guid.NewGuid():N}@test.com",
            DepartmentId = deptId,
            HireDate = new DateOnly(2025, 1, 1),
            UserId = otherUserId
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.NotNull(created);

        // Different employee tries to update
        var employeeClient = _factory.CreateClient();
        employeeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", callerToken);

        var updateResponse = await employeeClient.PutAsJsonAsync($"/api/employees/{created.Id}", new UpdateEmployeeDto
        {
            FirstName = "Hacker",
            LastName = "Attempt",
            Email = created.Email,
            DepartmentId = deptId
        });

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }
}
