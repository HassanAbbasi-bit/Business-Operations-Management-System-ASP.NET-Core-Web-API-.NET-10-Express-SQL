using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BusinessOperations.Application.DTOs.Auth;
using BusinessOperations.IntegrationTests.Fixtures;

namespace BusinessOperations.IntegrationTests.Controllers;

public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated_AndIssuesToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        string uniqueEmail = $"user_{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequestDto
        {
            Email = uniqueEmail,
            Password = "P@ssword123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.Token);
        Assert.Equal(uniqueEmail, authResponse.Email);
        Assert.Equal("Employee", authResponse.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"duplicate_{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequestDto
        {
            Email = email,
            Password = "P@ssword123!",
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Act 1: Initial registration
        var firstResponse = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act 2: Attempt duplicate registration
        var secondResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk_AndValidToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"login_{Guid.NewGuid():N}@example.com";
        const string password = "P@ssword123!";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = password,
            FirstName = "Mark",
            LastName = "Taylor"
        });

        // Act
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = password
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.Token);
        Assert.Equal(email, authResponse.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"wrongpwd_{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "P@ssword123!",
            FirstName = "David",
            LastName = "Miller"
        });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword999#"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidBearerToken_ReturnsOk_WithCurrentUserDetails()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"authme_{Guid.NewGuid():N}@example.com";
        const string password = "P@ssword123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = password,
            FirstName = "Sarah",
            LastName = "Connor"
        });

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResult);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

        // Act
        var meResponse = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var userDetails = await meResponse.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(userDetails);
        Assert.Equal(email, userDetails.Email);
        Assert.Equal("Employee", userDetails.Role);
    }

    [Fact]
    public async Task AdminOnly_WithEmployeeToken_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"emp_admincheck_{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "P@ssword123!",
            FirstName = "Regular",
            LastName = "Employee"
        });

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResult);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

        // Act (Employee attempting to access Admin-only endpoint)
        var response = await client.GetAsync("/api/auth/admin-only");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ManagerOnly_WithEmployeeToken_ReturnsForbidden()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"emp_mgrcheck_{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "P@ssword123!",
            FirstName = "Regular",
            LastName = "Employee"
        });

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResult);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);

        // Act (Employee attempting to access Manager-only endpoint)
        var response = await client.GetAsync("/api/auth/manager-only");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUsernameOrEmailProperty_ReturnsOk_AndValidToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"useralias_{Guid.NewGuid():N}@example.com";
        const string password = "P@ssword123!";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = password,
            FirstName = "Alias",
            LastName = "Test"
        });

        // Act: login using UsernameOrEmail property without explicit Email property
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            UsernameOrEmail = email,
            Password = password
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.NotEmpty(authResponse.Token);
        Assert.Equal(email, authResponse.Email);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsBadRequest_WithSafeErrorMessage()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"badpass_{Guid.NewGuid():N}@example.com";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "P@ssword123!",
            FirstName = "Security",
            LastName = "User"
        });

        // Act
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword999!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        string responseBody = await loginResponse.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", responseBody);
        // Verify no stack trace or SQL details leaked
        Assert.DoesNotContain("SELECT", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", responseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithExplicitManagerRole_AssignsManagerRole()
    {
        // Arrange
        var client = _factory.CreateClient();
        string email = $"explicit_mgr_{Guid.NewGuid():N}@example.com";

        // Act
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = email,
            Password = "P@ssword123!",
            FirstName = "Manager",
            LastName = "Candidate",
            Role = BusinessOperations.Domain.Enums.UserRole.Manager
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.Equal("Manager", authResponse.Role);
    }
}
