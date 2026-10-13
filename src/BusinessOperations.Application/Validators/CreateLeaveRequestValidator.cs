using BusinessOperations.Application.DTOs.LeaveRequests;
using FluentValidation;

namespace BusinessOperations.Application.Validators;

public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequestDto>
{
    public CreateLeaveRequestValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date cannot precede start date.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Leave reason is required.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
