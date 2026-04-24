using FluentValidation;
using Heimdall.Application.DTOs;

namespace Heimdall.Application.Validators;

public class RevokeRequestValidator : AbstractValidator<RevokeRequest>
{
    public RevokeRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MinimumLength(32).WithMessage("Invalid refresh token format.")
            .MaximumLength(512).WithMessage("Invalid refresh token format.")
            .Must(BeValidBase64).WithMessage("Refresh token must be valid Base64.");
    }

    private bool BeValidBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
