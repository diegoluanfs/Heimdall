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
            .MaximumLength(512).WithMessage("Invalid refresh token format.");
    }
}
