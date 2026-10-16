using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessOperations.Application.DTOs.Departments;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using BusinessOperations.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests.Controllers;

public class DepartmentsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DepartmentsEndpointTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/departments");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithEmployeeToken_ReturnsOk()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Employee));

        var response = await client.GetAsync("/api/departments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var departments = await response.Content.ReadFromJsonAsync<IReadOnlyList<DepartmentDto>>();
        Assert.NotNull(departments);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Employee));

        var response = await client.GetAsync($"/api/departments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonAdmin_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Employee));

        var response = await client.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = $"ForbiddenDept_{Guid.NewGuid():N}",
            Description = "Should fail"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAdmin_ValidData_ReturnsCreated_AndDepartment()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        string deptName = $"Engineering_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = deptName,
            Description = "Software Development Department"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<DepartmentDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(deptName, created.Name);
        Assert.Equal("Software Development Department", created.Description);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        string deptName = $"DuplicateDept_{Guid.NewGuid():N}";
        var payload = new CreateDepartmentDto
        {
            Name = deptName,
            Description = "Initial"
        };

        var first = await client.PostAsJsonAsync("/api/departments", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/departments", payload);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Update_WithAdmin_UpdatesDepartment()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken(UserRole.Admin));

        string deptName = $"Finance_{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = deptName,
            Description = "Initial finance desc"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<DepartmentDto>();
        Assert.NotNull(created);

        string updatedName = $"FinanceUpdated_{Guid.NewGuid():N}";
        var updateResponse = await client.PutAsJsonAsync($"/api/departments/{created.Id}", new UpdateDepartmentDto
        {
            Name = updatedName,
            Description = "Updated finance desc"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<DepartmentDto>();
        Assert.NotNull(updated);
        Assert.Equal(updatedName, updated.Name);
        Assert.Equal("Updated finance desc", updated.Description);
    }
}
