using BusinessOperations.Application.DTOs.Departments;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Exceptions;
using FluentValidation;

namespace BusinessOperations.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateDepartmentDto> _validator;
    private readonly IValidator<UpdateDepartmentDto> _updateValidator;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateDepartmentDto> validator,
        IValidator<UpdateDepartmentDto> updateValidator)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        return departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            ManagerEmployeeId = d.ManagerEmployeeId,
            EmployeeCount = d.Employees.Count,
            CreatedAtUtc = d.CreatedAtUtc
        }).ToList();
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken);
        if (department == null)
            return null;

        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            ManagerEmployeeId = department.ManagerEmployeeId,
            EmployeeCount = department.Employees.Count,
            CreatedAtUtc = department.CreatedAtUtc
        };
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        if (await _departmentRepository.ExistsByNameAsync(request.Name.Trim(), cancellationToken))
            throw new DomainException($"Department '{request.Name}' already exists.");

        var department = new Department(request.Name, request.Description, request.ManagerEmployeeId);
        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            ManagerEmployeeId = department.ManagerEmployeeId,
            EmployeeCount = 0,
            CreatedAtUtc = department.CreatedAtUtc
        };
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        var department = await _departmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Department not found.");

        if (!string.Equals(department.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if (await _departmentRepository.ExistsByNameAsync(request.Name.Trim(), cancellationToken))
                throw new DomainException($"Department '{request.Name}' already exists.");
        }

        department.UpdateDetails(request.Name, request.Description);
        department.AssignManager(request.ManagerEmployeeId);

        _departmentRepository.Update(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DepartmentDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            ManagerEmployeeId = department.ManagerEmployeeId,
            EmployeeCount = department.Employees.Count,
            CreatedAtUtc = department.CreatedAtUtc
        };
    }
}
