using FluentValidation;
using AuthService.Dtos;

namespace AuthService.Validators
{
    public class CreateUserValidators : AbstractValidator<CreateUserDto>
    {
        public CreateUserValidators()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(5).WithMessage("Username must be at least 5 characters.")
                .MaximumLength(80).WithMessage("Username must not exceed 80 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.")
                .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .MaximumLength(255).WithMessage("Password must not exceed 255 characters.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%.*?&])[A-Za-z\d@$!%.*?&]{6,}$")
                .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number and one special character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm password is required.")
                .Equal(x => x.Password).WithMessage("Passwords do not match.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
                .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");
        }
    }
}