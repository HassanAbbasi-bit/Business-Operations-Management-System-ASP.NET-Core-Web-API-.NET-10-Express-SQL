using BusinessOperations.Application.DTOs.Auth;
using FluentValidation;

namespace BusinessOperations.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.UsernameOrEmail))
            .WithMessage("Email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
