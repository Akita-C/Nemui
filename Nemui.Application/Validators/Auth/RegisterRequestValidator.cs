using FluentValidation;
using Nemui.Shared.Constants;
using Nemui.Shared.DTOs.Auth;

namespace Nemui.Application.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(AuthConstants.ValidationMessages.NameRequired)
            .MinimumLength(AuthConstants.Security.NameMinLength)
            .WithMessage(string.Format(AuthConstants.ValidationMessages.NameTooShort, AuthConstants.Security.NameMinLength))
            .MaximumLength(AuthConstants.Security.NameMaxLength)
            .WithMessage(string.Format(AuthConstants.ValidationMessages.NameTooLong, AuthConstants.Security.NameMaxLength))
            .Matches(@"^[a-zA-Z\s]+$").WithMessage(AuthConstants.ValidationMessages.NameInvalidCharacters);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AuthConstants.ValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthConstants.ValidationMessages.EmailInvalidFormat)
            .MaximumLength(AuthConstants.Security.EmailMaxLength)
            .WithMessage(string.Format(AuthConstants.ValidationMessages.EmailTooLong, AuthConstants.Security.EmailMaxLength));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthConstants.ValidationMessages.PasswordRequired)
            .MinimumLength(AuthConstants.Security.PasswordMinLength)
            .WithMessage(string.Format(AuthConstants.ValidationMessages.PasswordTooShort, AuthConstants.Security.PasswordMinLength))
            .MaximumLength(AuthConstants.Security.PasswordMaxLength)
            .WithMessage(string.Format(AuthConstants.ValidationMessages.PasswordTooLong, AuthConstants.Security.PasswordMaxLength))
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$")
            .WithMessage(AuthConstants.ValidationMessages.PasswordComplexity);

        RuleFor(x => x.PasswordConfirmation)
            .NotEmpty().WithMessage(AuthConstants.ValidationMessages.PasswordConfirmationRequired)
            .Equal(x => x.Password).WithMessage(AuthConstants.ValidationMessages.PasswordsDoNotMatch);
    }
}