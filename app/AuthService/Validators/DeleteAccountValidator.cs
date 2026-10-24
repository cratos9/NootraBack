using AuthService.Dtos;
using FluentValidation;

namespace AuthService.Validators
{
    public class DeleteAccountDtoValidator
        : AbstractValidator<DeleteAccountDto>
    {
        public DeleteAccountDtoValidator()
        {
            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("The password is required.");
        }
    }
}