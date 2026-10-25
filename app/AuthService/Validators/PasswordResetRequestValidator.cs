using AuthService.Dtos;
using FluentValidation;

namespace AuthService.Validators;

public class PasswordResetRequestValidator
    : AbstractValidator<PasswordResetRequestDto>
{
    public PasswordResetRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("The email address is not valid.")
            .MaximumLength(254)
            .WithMessage("The email cannot exceed 254 characters.");
    }
}