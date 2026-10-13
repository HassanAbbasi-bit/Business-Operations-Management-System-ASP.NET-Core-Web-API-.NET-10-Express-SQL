using BusinessOperations.Application.DTOs.Employees;
using FluentValidation;

namespace BusinessOperations.Application.Validators;

public class UpdateEmployeeValidator : AbstractValidator<UpdateEmployeeDto>
{
    public UpdateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required.");

        When(x => x.AnnualLeaveBalanceDays.HasValue, () =>
        {
            RuleFor(x => x.AnnualLeaveBalanceDays!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Leave balance cannot be negative.");
        });
    }
}
