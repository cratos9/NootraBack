using FluentValidation;
using AuthService.Dtos;

namespace AuthService.Validators
{
    public class LoginUserValidator : AbstractValidator<LoginUserDto>
    {
        public LoginUserValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("The email is required.")
                .EmailAddress().WithMessage("The email address is not valid.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("The password is required.")
                .MinimumLength(6).WithMessage("The password must be at least 6 characters.");
        }
    }
}