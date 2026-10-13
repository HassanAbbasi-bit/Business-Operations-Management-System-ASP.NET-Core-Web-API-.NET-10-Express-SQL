using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessOperations.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
