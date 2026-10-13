using BusinessOperations.Domain.Entities;

namespace BusinessOperations.Application.Interfaces.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    void Update(Employee employee);
}
