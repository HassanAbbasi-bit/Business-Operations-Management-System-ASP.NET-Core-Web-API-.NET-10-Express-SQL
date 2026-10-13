using BusinessOperations.Application.DTOs.Expenses;
using FluentValidation;

namespace BusinessOperations.Application.Validators;

public class CreateExpenseValidator : AbstractValidator<CreateExpenseDto>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Expense amount must be greater than zero.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Expense description is required.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.ExpenseDate)
            .NotEmpty().WithMessage("Expense date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Expense date cannot be in the future.");
    }
}
