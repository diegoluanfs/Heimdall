using FluentValidation;
using Heimdall.Application.DTOs;

namespace Heimdall.Application.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MinimumLength(3).WithMessage("Project name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Project name must not exceed 100 characters.");

        RuleFor(x => x.Audience)
            .NotEmpty().WithMessage("Audience is required.")
            .MinimumLength(3).WithMessage("Audience must be at least 3 characters.")
            .MaximumLength(256).WithMessage("Audience must not exceed 256 characters.")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Audience must contain only lowercase letters, numbers, and hyphens.");
    }
}
