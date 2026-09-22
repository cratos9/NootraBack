using FluentValidation;
using AuthService.Dtos;

namespace AuthService.Validators
{
    public class CreateUserValidators : AbstractValidator<CreateUserDto>
    {
        public CreateUserValidators()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
                .MinimumLength(5).WithMessage("El nombre de usuario debe tener al menos 5 caracteres.")
                .MaximumLength(80).WithMessage("El nombre de usuario no debe exceder 80 caracteres.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
                .EmailAddress().WithMessage("Formato de correo electrónico inválido.")
                .MaximumLength(100).WithMessage("El correo electrónico no debe exceder 100 caracteres.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
                .MaximumLength(255).WithMessage("La contraseña no debe exceder 255 caracteres.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%.*?&])[A-Za-z\d@$!%.*?&]{6,}$")
                .WithMessage("La contraseña debe contener al menos una letra mayúscula, una letra minúscula, un número y un carácter especial.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirmar contraseña es obligatorio.")
                .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden.");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("El nombre completo es obligatorio.")
                .MinimumLength(2).WithMessage("El nombre completo debe tener al menos 2 caracteres.")
                .MaximumLength(100).WithMessage("El nombre completo no debe exceder 100 caracteres.");
        }
    }
}