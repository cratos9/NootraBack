using AuthService.Dtos;
using FluentValidation;

namespace AuthService.Validators;

public class PasswordResetValidator
    : AbstractValidator<PasswordResetDto>
{
    public PasswordResetValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("The email address is not valid.")
            .MaximumLength(254)
            .WithMessage("The email cannot exceed 254 characters.");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("The token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("The new password is required.")
            .MinimumLength(8)
            .WithMessage("The password must be at least 8 characters.")
            .Matches("[A-Z]")
            .WithMessage("The password must contain an uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("The password must contain a lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("The password must contain a number.");
    }
}