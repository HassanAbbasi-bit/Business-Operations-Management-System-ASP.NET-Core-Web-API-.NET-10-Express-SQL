using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Data;

public static class DatabaseSeeder
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher, CancellationToken cancellationToken = default)
    {
        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            // 1. Ensure core departments exist
            var opsDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Operations", cancellationToken);
            if (opsDept == null)
            {
                opsDept = new Department("Operations", "Core Business Operations & Strategy");
                await context.Departments.AddAsync(opsDept, cancellationToken);
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    context.ChangeTracker.Clear();
                    opsDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Operations", cancellationToken);
                }
            }

            var engDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Engineering", cancellationToken);
            if (engDept == null)
            {
                engDept = new Department("Engineering", "Systems, Platform & Development");
                await context.Departments.AddAsync(engDept, cancellationToken);
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    context.ChangeTracker.Clear();
                }
            }

            var finDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Finance", cancellationToken);
            if (finDept == null)
            {
                finDept = new Department("Finance", "Corporate Finance & Reimbursements");
                await context.Departments.AddAsync(finDept, cancellationToken);
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    context.ChangeTracker.Clear();
                }
            }

            if (opsDept == null) return;

            // 2. Ensure Admin demo account exists
            var adminUser = await context.Users.Include(u => u.Employee).FirstOrDefaultAsync(u => u.Email == "admin@operix.com", cancellationToken);
            if (adminUser == null)
            {
                string hash = passwordHasher.HashPassword("Admin123!");
                adminUser = new User("admin@operix.com", hash, UserRole.Admin);
                await context.Users.AddAsync(adminUser, cancellationToken);
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    var adminEmp = new Employee("Admin", "User", "admin@operix.com", opsDept.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)), null, adminUser.Id, 25);
                    await context.Employees.AddAsync(adminEmp, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    context.ChangeTracker.Clear();
                }
            }

            // 3. Ensure Manager demo account exists
            var managerUser = await context.Users.Include(u => u.Employee).FirstOrDefaultAsync(u => u.Email == "manager@operix.com", cancellationToken);
            if (managerUser == null)
            {
                string hash = passwordHasher.HashPassword("Manager123!");
                managerUser = new User("manager@operix.com", hash, UserRole.Manager);
                await context.Users.AddAsync(managerUser, cancellationToken);
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    var managerEmp = new Employee("Sarah", "Jenkins", "manager@operix.com", opsDept.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)), null, managerUser.Id, 22);
                    await context.Employees.AddAsync(managerEmp, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);

                    opsDept.AssignManager(managerEmp.Id);
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    context.ChangeTracker.Clear();
                }
            }

            // 4. Ensure Employee demo account exists
            var employeeUser = await context.Users.Include(u => u.Employee).FirstOrDefaultAsync(u => u.Email == "employee@operix.com", cancellationToken);
            if (employeeUser == null)
            {
                string hash = passwordHasher.HashPassword("Employee123!");
                employeeUser = new User("employee@operix.com", hash, UserRole.Employee);
                await context.Users.AddAsync(employeeUser, cancellationToken);
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    var staffEmp = new Employee("Alex", "Rivera", "employee@operix.com", opsDept.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)), null, employeeUser.Id, 20);
                    await context.Employees.AddAsync(staffEmp, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    context.ChangeTracker.Clear();
                }
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }
}
