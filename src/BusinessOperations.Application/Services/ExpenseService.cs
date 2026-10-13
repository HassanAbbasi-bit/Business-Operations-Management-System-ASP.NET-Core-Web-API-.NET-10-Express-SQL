using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Expenses;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;
using FluentValidation;

namespace BusinessOperations.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateExpenseDto> _createValidator;

    public ExpenseService(
        IExpenseRepository expenseRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateExpenseDto> createValidator)
    {
        _expenseRepository = expenseRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
    }

    public async Task<ExpenseDto> SubmitAsync(Guid currentUserId, CreateExpenseDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        var employee = await _employeeRepository.GetByUserIdAsync(currentUserId, cancellationToken)
            ?? throw new DomainException("Employee profile not found. User must have an associated employee profile to submit expenses.");

        if (!employee.IsActive)
            throw new DomainException("An inactive employee cannot submit expense reimbursements.");

        var expense = new Expense(
            employee.Id,
            request.Category,
            request.Amount,
            request.Description,
            request.ExpenseDate,
            request.Currency);

        await _expenseRepository.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(expense, employee.FullName, employee.Department?.Name);
    }

    public async Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);
        return expense == null ? null : MapToDto(expense);
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetMyExpensesAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(currentUserId, cancellationToken);
        if (employee == null)
            return Array.Empty<ExpenseDto>();

        var list = await _expenseRepository.GetByEmployeeIdAsync(employee.Id, cancellationToken);
        return list.Select(e => MapToDto(e)).ToList();
    }

    public async Task<PagedResult<ExpenseDto>> GetPagedAsync(
        ExpenseStatus? status,
        ExpenseCategory? category,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paged = await _expenseRepository.GetPagedAsync(status, category, employeeId, departmentId, pageNumber, pageSize, cancellationToken);
        var dtos = paged.Items.Select(e => MapToDto(e)).ToList();
        return new PagedResult<ExpenseDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    public async Task<ExpenseDto> ApproveAsync(Guid id, Guid reviewerUserId, ReviewExpenseDto? reviewDto, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Expense submission not found.");

        var reviewer = await _employeeRepository.GetByUserIdAsync(reviewerUserId, cancellationToken)
            ?? throw new DomainException("Reviewer employee profile not found.");

        if (reviewer.Id == expense.EmployeeId)
            throw new DomainException("Employees are not permitted to approve their own expense submissions.");

        expense.Approve(reviewer.Id, reviewDto?.Notes);

        _expenseRepository.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(expense, expense.Employee?.FullName, expense.Employee?.Department?.Name, reviewer.FullName);
    }

    public async Task<ExpenseDto> RejectAsync(Guid id, Guid reviewerUserId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Expense submission not found.");

        var reviewer = await _employeeRepository.GetByUserIdAsync(reviewerUserId, cancellationToken)
            ?? throw new DomainException("Reviewer employee profile not found.");

        if (reviewer.Id == expense.EmployeeId)
            throw new DomainException("Employees are not permitted to review their own expense submissions.");

        expense.Reject(reviewer.Id, rejectionReason);

        _expenseRepository.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(expense, expense.Employee?.FullName, expense.Employee?.Department?.Name, reviewer.FullName);
    }

    public async Task<ExpenseDto> CancelAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Expense submission not found.");

        var callerEmployee = await _employeeRepository.GetByUserIdAsync(currentUserId, cancellationToken)
            ?? throw new DomainException("Employee profile not found.");

        expense.Cancel(callerEmployee.Id);

        _expenseRepository.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(expense);
    }

    private static ExpenseDto MapToDto(Expense e, string? employeeName = null, string? departmentName = null, string? reviewerName = null)
    {
        return new ExpenseDto
        {
            Id = e.Id,
            EmployeeId = e.EmployeeId,
            EmployeeName = employeeName ?? e.Employee?.FullName,
            DepartmentName = departmentName ?? e.Employee?.Department?.Name,
            Category = e.Category,
            Amount = e.Amount,
            Currency = e.Currency,
            Description = e.Description,
            ExpenseDate = e.ExpenseDate,
            Status = e.Status,
            ReviewedByEmployeeId = e.ReviewedByEmployeeId,
            ReviewedByEmployeeName = reviewerName ?? e.ReviewedByEmployee?.FullName,
            ReviewedAtUtc = e.ReviewedAtUtc,
            ReviewNotes = e.ReviewNotes,
            CreatedAtUtc = e.CreatedAtUtc
        };
    }
}
