using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Employees;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Exceptions;
using FluentValidation;

namespace BusinessOperations.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateEmployeeDto> _createValidator;
    private readonly IValidator<UpdateEmployeeDto> _updateValidator;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateEmployeeDto> createValidator,
        IValidator<UpdateEmployeeDto> updateValidator)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<EmployeeDto>> GetPagedAsync(
        string? searchTerm,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paged = await _employeeRepository.GetPagedAsync(searchTerm, departmentId, pageNumber, pageSize, cancellationToken);
        var dtos = paged.Items.Select(MapToDto).ToList();

        return new PagedResult<EmployeeDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(userId, cancellationToken);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        var dept = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
            ?? throw new DomainException($"Department with ID '{request.DepartmentId}' does not exist.");

        if (await _employeeRepository.ExistsByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken))
            throw new DomainException($"An employee with email '{request.Email}' already exists.");

        var employee = new Employee(
            request.FirstName,
            request.LastName,
            request.Email,
            request.DepartmentId,
            request.HireDate,
            request.ManagerId,
            request.UserId,
            request.AnnualLeaveBalanceDays);

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(employee, dept.Name);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Employee not found.");

        if (request.DepartmentId != Guid.Empty && request.DepartmentId != employee.DepartmentId)
        {
            var dept = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken)
                ?? throw new DomainException("Specified department does not exist.");
            employee.TransferDepartment(request.DepartmentId);
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
        {
            employee.SetNames(
                string.IsNullOrWhiteSpace(request.FirstName) ? employee.FirstName : request.FirstName,
                string.IsNullOrWhiteSpace(request.LastName) ? employee.LastName : request.LastName);
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(request.Email.Trim(), employee.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await _employeeRepository.ExistsByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken))
                throw new DomainException($"An employee with email '{request.Email}' already exists.");
            employee.SetEmail(request.Email);
        }

        if (request.ManagerId.HasValue)
        {
            employee.AssignManager(request.ManagerId.Value == Guid.Empty ? null : request.ManagerId);
        }

        if (request.AnnualLeaveBalanceDays.HasValue)
        {
            employee.SetLeaveBalance(request.AnnualLeaveBalanceDays.Value);
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) employee.Activate();
            else employee.Deactivate();
        }

        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(employee);
    }

    private static EmployeeDto MapToDto(Employee e) => MapToDto(e, e.Department?.Name);

    private static EmployeeDto MapToDto(Employee e, string? departmentName)
    {
        return new EmployeeDto
        {
            Id = e.Id,
            UserId = e.UserId,
            FirstName = e.FirstName,
            LastName = e.LastName,
            FullName = e.FullName,
            Email = e.Email,
            DepartmentId = e.DepartmentId,
            DepartmentName = departmentName ?? e.Department?.Name,
            ManagerId = e.ManagerId,
            ManagerName = e.Manager?.FullName,
            HireDate = e.HireDate,
            AnnualLeaveBalanceDays = e.AnnualLeaveBalanceDays,
            IsActive = e.IsActive
        };
    }
}
