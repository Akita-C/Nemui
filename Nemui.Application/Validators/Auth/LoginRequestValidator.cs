using FluentValidation;
using Nemui.Shared.Constants;
using Nemui.Shared.DTOs.Auth;

namespace Nemui.Application.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
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
            .WithMessage(string.Format(AuthConstants.ValidationMessages.PasswordTooLong, AuthConstants.Security.PasswordMaxLength));
    }
}