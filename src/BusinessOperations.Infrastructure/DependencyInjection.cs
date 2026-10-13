using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, Repositories.UserRepository>();
        services.AddScoped<IEmployeeRepository, Repositories.EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, Repositories.DepartmentRepository>();
        services.AddScoped<IUnitOfWork, Repositories.UnitOfWork>();

        services.AddSingleton<IPasswordHasher, Services.PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, Services.JwtTokenGenerator>();

        return services;
    }
}
