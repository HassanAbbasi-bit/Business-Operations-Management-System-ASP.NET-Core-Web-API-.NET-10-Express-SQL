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
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<ITaskItemService, TaskItemService>();
        services.AddScoped<IExpenseService, ExpenseService>();

        return services;
    }
}
