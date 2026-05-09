using FluentValidation;
using Karamchari.Api.BFF.Common;

namespace Karamchari.Api.Validation;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class StartPayrollRunRequestValidator : AbstractValidator<StartPayrollRunRequest>
{
    public StartPayrollRunRequestValidator()
    {
        RuleFor(x => x.PeriodName)
            .NotEmpty().WithMessage("Period name is required.")
            .Matches(@"^(April|May|June|July|August|September|October|November|December|January|February|March) \d{4}$")
            .WithMessage("Period name must be in 'Month YYYY' format (e.g., 'May 2026').");
    }
}
