using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.LeaveRequests;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;
using FluentValidation;

namespace BusinessOperations.Application.Services;

public class LeaveRequestService : ILeaveRequestService
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateLeaveRequestDto> _createValidator;

    public LeaveRequestService(
        ILeaveRequestRepository leaveRequestRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateLeaveRequestDto> createValidator)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    public async Task<LeaveRequestDto> SubmitAsync(Guid currentUserId, CreateLeaveRequestDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        Employee? employee;
        if (request.EmployeeId.HasValue && request.EmployeeId.Value != Guid.Empty)
        {
            employee = await _employeeRepository.GetByIdAsync(request.EmployeeId.Value, cancellationToken);
        }
        else
        {
            employee = await _employeeRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        }

        if (employee == null)
            throw new DomainException("Employee profile not found. User must have an associated employee profile to submit leave requests.");

        if (!employee.IsActive)
            throw new DomainException("An inactive employee cannot submit leave requests.");

        int daysCount = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        if (daysCount <= 0)
            throw new DomainException("Leave request must span at least 1 day.");

        if (employee.AnnualLeaveBalanceDays < daysCount)
            throw new DomainException($"Insufficient leave balance. Available: {employee.AnnualLeaveBalanceDays}, Requested: {daysCount}.");

        bool hasOverlap = await _leaveRequestRepository.HasActiveOverlappingRequestAsync(
            employee.Id, request.StartDate, request.EndDate, null, cancellationToken);

        if (hasOverlap)
            throw new DomainException("Employee already has an active or pending leave request covering these dates.");

        var leaveRequest = new LeaveRequest(employee.Id, request.StartDate, request.EndDate, request.Reason);
        await _leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(leaveRequest, employee.FullName, employee.Department?.Name);
    }

    public async Task<LeaveRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken);
        return leaveRequest == null ? null : MapToDto(leaveRequest);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        if (employee == null)
            return Array.Empty<LeaveRequestDto>();

        var list = await _leaveRequestRepository.GetByEmployeeIdAsync(employee.Id, cancellationToken);
        return list.Select(r => MapToDto(r)).ToList();
    }

    public async Task<PagedResult<LeaveRequestDto>> GetPagedAsync(
        LeaveStatus? status,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paged = await _leaveRequestRepository.GetPagedAsync(status, employeeId, departmentId, pageNumber, pageSize, cancellationToken);
        var dtos = paged.Items.Select(r => MapToDto(r)).ToList();
        return new PagedResult<LeaveRequestDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    public async Task<LeaveRequestDto> ApproveAsync(Guid id, Guid reviewerUserId, ReviewLeaveRequestDto? reviewDto, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Leave request not found.");

        var reviewer = await _employeeRepository.GetByUserIdAsync(reviewerUserId, cancellationToken)
            ?? throw new DomainException("Reviewer employee profile not found.");

        if (reviewer.Id == leaveRequest.EmployeeId)
            throw new DomainException("Employees are not permitted to approve their own leave requests.");

        var employee = await _employeeRepository.GetByIdAsync(leaveRequest.EmployeeId, cancellationToken)
            ?? throw new DomainException("Employee profile not found.");

        leaveRequest.Approve(reviewer.Id, reviewDto?.Notes);
        employee.DeductLeaveDays(leaveRequest.DaysCount);

        _leaveRequestRepository.Update(leaveRequest);
        _employeeRepository.Update(employee);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(leaveRequest, employee.FullName, employee.Department?.Name, reviewer.FullName);
    }

    public async Task<LeaveRequestDto> RejectAsync(Guid id, Guid reviewerUserId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Leave request not found.");

        var reviewer = await _employeeRepository.GetByUserIdAsync(reviewerUserId, cancellationToken)
            ?? throw new DomainException("Reviewer employee profile not found.");

        if (reviewer.Id == leaveRequest.EmployeeId)
            throw new DomainException("Employees are not permitted to review their own leave requests.");

        leaveRequest.Reject(reviewer.Id, rejectionReason);

        _leaveRequestRepository.Update(leaveRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(leaveRequest, leaveRequest.Employee?.FullName, leaveRequest.Employee?.Department?.Name, reviewer.FullName);
    }

    public async Task<LeaveRequestDto> CancelAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _leaveRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Leave request not found.");

        var callerEmployee = await _employeeRepository.GetByUserIdAsync(currentUserId, cancellationToken)
            ?? throw new DomainException("Employee profile not found.");

        leaveRequest.Cancel(callerEmployee.Id);

        _leaveRequestRepository.Update(leaveRequest);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(leaveRequest);
    }

    private static LeaveRequestDto MapToDto(LeaveRequest r, string? employeeName = null, string? departmentName = null, string? reviewerName = null)
    {
        return new LeaveRequestDto
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeName = employeeName ?? r.Employee?.FullName,
            DepartmentName = departmentName ?? r.Employee?.Department?.Name,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            DaysCount = r.DaysCount,
            Reason = r.Reason,
            Status = r.Status,
            ReviewedByEmployeeId = r.ReviewedByEmployeeId,
            ReviewedByEmployeeName = reviewerName ?? r.ReviewedByEmployee?.FullName,
            ReviewedAtUtc = r.ReviewedAtUtc,
            ReviewNotes = r.ReviewNotes,
            CreatedAtUtc = r.CreatedAtUtc
        };
    }
}
