using BusinessOperations.Application.Interfaces;
using BusinessOperations.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestConnectionString = "Server=.\\SQLEXPRESS;Database=BusinessOperations_TestDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    static CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", TestConnectionString);
    }

    private static readonly object InitLock = new();
    private static bool _initialized = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["JwtSettings:Issuer"] = "BusinessOperationsApi",
                ["JwtSettings:Audience"] = "BusinessOperationsClient",
                ["JwtSettings:Secret"] = "BusinessOperations_Very_Secure_Development_Secret_Key_At_Least_32_Chars!",
                ["JwtSettings:ExpirationMinutes"] = "60"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContextOptions<ApplicationDbContext> and ApplicationDbContext registrations
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var dbContextServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (dbContextServiceDescriptor != null)
            {
                services.Remove(dbContextServiceDescriptor);
            }

            AssertSafeTestDatabase(TestConnectionString);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(TestConnectionString));

            // Ensure test database is migrated and seeded once
            var sp = services.BuildServiceProvider();
            lock (InitLock)
            {
                if (!_initialized)
                {
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

                    db.Database.Migrate();
                    DatabaseSeeder.SeedAsync(db, hasher).GetAwaiter().GetResult();
                    _initialized = true;
                }
            }
        });
    }

    /// <summary>
    /// Strict guard refusing execution if connection targets normal development or production databases.
    /// </summary>
    public static void AssertSafeTestDatabase(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("CRITICAL SAFETY GUARD: Connection string is empty. Refusing to run tests.");
        }

        // Must explicitly contain 'test' or '_test' in database name
        bool isTestDb = connectionString.Contains("TestDb", StringComparison.OrdinalIgnoreCase) ||
                        connectionString.Contains("_Test", StringComparison.OrdinalIgnoreCase) ||
                        connectionString.Contains("Test", StringComparison.OrdinalIgnoreCase);

        // Explicitly forbid running against normal development database BusinessOperationsDb without Test suffix
        bool isDevDb = connectionString.Contains("Database=BusinessOperationsDb", StringComparison.OrdinalIgnoreCase) ||
                       connectionString.Contains("Initial Catalog=BusinessOperationsDb", StringComparison.OrdinalIgnoreCase);

        if (!isTestDb || isDevDb)
        {
            throw new InvalidOperationException(
                $"CRITICAL SAFETY GUARD: Integration tests must target a dedicated test database (must contain 'Test' in database name). " +
                $"Attempted to run against non-test or development database: '{connectionString}'. Execution blocked.");
        }
    }
}
