using AuthService.Dtos;
using FluentValidation;

namespace AuthService.Validators
{
    public class ChangePasswordDtoValidator
        : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty()
                .WithMessage("The current password is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithMessage("The new password is required.")
                .MinimumLength(6)
                .WithMessage("The password must be at least 6 characters.")
                .MaximumLength(255)
                .WithMessage("The password cannot exceed 255 characters.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$")
                .WithMessage(
                    "The password must include uppercase, lowercase, numbers, and special characters."
                )
                .Must((dto, password) => password != dto.CurrentPassword)
                .WithMessage("The new password must be different from the current password.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("The confirm password is required.")
                .Equal(x => x.NewPassword)
                .WithMessage("The confirm password does not match the new password.");
        }
    }
}