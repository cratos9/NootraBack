using AuthService.Dtos;
using FluentValidation;

namespace AuthService.Validators
{
    public class UpdateAccountDtoValidator
        : AbstractValidator<UpdateAccountDto>
    {
        public UpdateAccountDtoValidator()
        {
            RuleFor(x => x.Username)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("The username cannot be empty.")
                .MinimumLength(5)
                .WithMessage("The username must be at least 5 characters long.")
                .MaximumLength(80)
                .WithMessage("The username cannot exceed 80 characters.")
                .When(x => x.Username is not null);

            RuleFor(x => x.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("The email cannot be empty.")
                .EmailAddress()
                .WithMessage("The email is not in a valid format.")
                .MaximumLength(100)
                .WithMessage("The email cannot exceed 100 characters.")
                .When(x => x.Email is not null);

            RuleFor(x => x.FullName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("The fullName cannot be empty.")
                .MinimumLength(2)
                .WithMessage("The fullName must be at least 2 characters long.")
                .MaximumLength(100)
                .WithMessage("The fullName cannot exceed 100 characters.")
                .When(x => x.FullName is not null);

            RuleFor(x => x.Biography)
                .MaximumLength(2000)
                .WithMessage("The biography cannot exceed 2000 characters.")
                .When(x => x.Biography is not null);

            RuleFor(x => x.ProfilePictureUrl)
                .MaximumLength(255)
                .WithMessage("The URL of the image cannot exceed 255 characters.")
                .When(x => x.ProfilePictureUrl is not null);

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(30)
                .WithMessage("The phone number cannot exceed 30 characters.")
                .When(x => x.PhoneNumber is not null);

            RuleFor(x => x.Country)
                .MaximumLength(100)
                .WithMessage("The country cannot exceed 100 characters.")
                .When(x => x.Country is not null);

            RuleFor(x => x.City)
                .MaximumLength(100)
                .WithMessage("The city cannot exceed 100 characters.")
                .When(x => x.City is not null);

            RuleFor(x => x.Institution)
                .MaximumLength(200)
                .WithMessage("The institution cannot exceed 200 characters.")
                .When(x => x.Institution is not null);

            RuleFor(x => x.Career)
                .MaximumLength(150)
                .WithMessage("The career cannot exceed 150 characters.")
                .When(x => x.Career is not null);

            RuleFor(x => x.StudentId)
                .MaximumLength(100)
                .WithMessage("The student ID cannot exceed 100 characters.")
                .When(x => x.StudentId is not null);
        }
    }
}