using BusinessOperations.Application.DTOs.Departments;

namespace BusinessOperations.Application.Interfaces;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto request, CancellationToken cancellationToken = default);
}
