using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Employees;

namespace BusinessOperations.Application.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeDto>> GetPagedAsync(string? searchTerm, Guid? departmentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default);
}
