using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Departments;
using BusinessOperations.Application.DTOs.Employees;
using BusinessOperations.Application.DTOs.Tasks;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using BusinessOperations.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests.Controllers;

public class TasksEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TasksEndpointTests(CustomWebApplicationFactory factory)
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

    private async Task<(Guid UserId, Guid EmployeeId, string Token, HttpClient Client)> CreateEmployeeWithUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtGen = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();

        string email = $"taskuser_{Guid.NewGuid():N}@test.com";
        var user = new User(email, hasher.HashPassword("P@ssword123!"), UserRole.Employee);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var (token, _) = jwtGen.GenerateToken(user);

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateAdminToken());

        var deptResponse = await adminClient.PostAsJsonAsync("/api/departments", new CreateDepartmentDto
        {
            Name = $"TaskDept_{Guid.NewGuid():N}",
            Description = "Department for task testing"
        });
        deptResponse.EnsureSuccessStatusCode();
        var deptDto = await deptResponse.Content.ReadFromJsonAsync<DepartmentDto>();

        var empResponse = await adminClient.PostAsJsonAsync("/api/employees", new CreateEmployeeDto
        {
            FirstName = "Task",
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
    public async Task GetPaged_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreated_AndTaskItem()
    {
        var (_, empId, _, client) = await CreateEmployeeWithUserAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Implement Feature X",
            Description = "Build and test feature X according to spec.",
            Priority = TaskPriority.High,
            DueDateUtc = DateTime.UtcNow.AddDays(7),
            AssignedToEmployeeId = empId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Implement Feature X", created.Title);
        Assert.Equal(TaskPriority.High, created.Priority);
        Assert.Equal(TaskItemStatus.Todo, created.Status);
        Assert.Equal(empId, created.AssignedToEmployeeId);
    }

    [Fact]
    public async Task Create_WithoutTitle_ReturnsBadRequest()
    {
        var (_, _, _, client) = await CreateEmployeeWithUserAsync();

        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "",
            Description = "Missing title"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ValidData_UpdatesTaskProperties()
    {
        var (_, empId, _, client) = await CreateEmployeeWithUserAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Initial Title",
            Description = "Initial Description",
            Priority = TaskPriority.Low
        });
        var created = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/tasks/{created!.Id}", new UpdateTaskItemDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Priority = TaskPriority.Urgent,
            AssignedToEmployeeId = empId
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated Description", updated.Description);
        Assert.Equal(TaskPriority.Urgent, updated.Priority);
        Assert.Equal(empId, updated.AssignedToEmployeeId);
    }

    [Fact]
    public async Task WorkflowLifecycle_FullStateTransitions_Succeed()
    {
        var (_, empId, _, client) = await CreateEmployeeWithUserAsync();

        // 1. Create -> Todo
        var createResponse = await client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Lifecycle Task",
            Description = "Testing state machine transitions",
            AssignedToEmployeeId = empId
        });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal(TaskItemStatus.Todo, task!.Status);

        // 2. Start -> InProgress
        var startResponse = await client.PostAsync($"/api/tasks/{task.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var inProgress = await startResponse.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal(TaskItemStatus.InProgress, inProgress!.Status);

        // 3. Review -> InReview
        var reviewResponse = await client.PostAsync($"/api/tasks/{task.Id}/review", null);
        Assert.Equal(HttpStatusCode.OK, reviewResponse.StatusCode);
        var inReview = await reviewResponse.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal(TaskItemStatus.InReview, inReview!.Status);

        // 4. Request Changes -> back to InProgress
        var changesResponse = await client.PostAsync($"/api/tasks/{task.Id}/request-changes", null);
        Assert.Equal(HttpStatusCode.OK, changesResponse.StatusCode);
        var changesRequested = await changesResponse.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal(TaskItemStatus.InProgress, changesRequested!.Status);

        // 5. Complete -> Done
        var completeResponse = await client.PostAsync($"/api/tasks/{task.Id}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var done = await completeResponse.Content.ReadFromJsonAsync<TaskItemDto>();
        Assert.Equal(TaskItemStatus.Done, done!.Status);
        Assert.NotNull(done.CompletedAtUtc);
    }

    [Fact]
    public async Task StartProgress_FromInvalidState_ReturnsBadRequest()
    {
        var (_, empId, _, client) = await CreateEmployeeWithUserAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Invalid Transition Task",
            AssignedToEmployeeId = empId
        });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

        // Transition once to InProgress
        await client.PostAsync($"/api/tasks/{task!.Id}/start", null);

        // Attempting to start progress again from InProgress is an invalid state transition
        var secondStart = await client.PostAsync($"/api/tasks/{task.Id}/start", null);
        Assert.Equal(HttpStatusCode.BadRequest, secondStart.StatusCode);
    }

    [Fact]
    public async Task Cancel_WhenDone_ReturnsBadRequest()
    {
        var (_, empId, _, client) = await CreateEmployeeWithUserAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Task to complete then cancel",
            AssignedToEmployeeId = empId
        });
        var task = await createResponse.Content.ReadFromJsonAsync<TaskItemDto>();

        // Complete the task
        await client.PostAsync($"/api/tasks/{task!.Id}/complete", null);

        // Attempting to cancel a completed task is invalid
        var cancelResponse = await client.PostAsync($"/api/tasks/{task.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.BadRequest, cancelResponse.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithEmployeeToken_ScopesToOwnAssignedTasks()
    {
        var (_, emp1Id, _, client1) = await CreateEmployeeWithUserAsync();
        var (_, emp2Id, _, client2) = await CreateEmployeeWithUserAsync();

        // Create task for emp1
        await client1.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Task For Emp1",
            AssignedToEmployeeId = emp1Id
        });

        // Create task for emp2
        await client2.PostAsJsonAsync("/api/tasks", new CreateTaskItemDto
        {
            Title = "Task For Emp2",
            AssignedToEmployeeId = emp2Id
        });

        // Emp1 lists tasks without specifying assignedToId query param
        var response = await client1.GetAsync("/api/tasks?pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResult<TaskItemDto>>();
        Assert.NotNull(paged);
        Assert.All(paged.Items, t => Assert.Equal(emp1Id, t.AssignedToEmployeeId));
    }
}
