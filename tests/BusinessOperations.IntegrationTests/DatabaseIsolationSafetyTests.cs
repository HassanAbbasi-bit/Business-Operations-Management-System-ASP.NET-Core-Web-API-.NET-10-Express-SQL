using BusinessOperations.Infrastructure.Data;
using BusinessOperations.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests;

public class DatabaseIsolationSafetyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DatabaseIsolationSafetyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Factory_ConfiguresTestDatabase_NotDevelopmentDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connectionString = db.Database.GetConnectionString();

        Assert.NotNull(connectionString);
        Assert.Contains("BusinessOperations_TestDb", connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Database=BusinessOperationsDb;", connectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AssertSafeTestDatabase_RejectsOrdinaryDevelopmentDatabase_ThrowsInvalidOperationException()
    {
        const string devConn = "Server=.\\SQLEXPRESS;Database=BusinessOperationsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomWebApplicationFactory.AssertSafeTestDatabase(devConn));

        Assert.Contains("CRITICAL SAFETY GUARD", ex.Message);
        Assert.Contains("BusinessOperationsDb", ex.Message);
    }

    [Fact]
    public void AssertSafeTestDatabase_RejectsEmptyOrNullConnection_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CustomWebApplicationFactory.AssertSafeTestDatabase(""));

        Assert.Contains("CRITICAL SAFETY GUARD", ex.Message);
    }

    [Fact]
    public void AssertSafeTestDatabase_AcceptsValidIsolatedTestDatabase()
    {
        const string testConn = "Server=.\\SQLEXPRESS;Database=BusinessOperations_TestDb;Trusted_Connection=True;";
        var exception = Record.Exception(() => CustomWebApplicationFactory.AssertSafeTestDatabase(testConn));
        Assert.Null(exception);
    }
}
