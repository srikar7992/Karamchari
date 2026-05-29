using FluentValidation;
using Karamchari.HR.Contracts.Employees;

namespace Karamchari.Api.Validation;

/// <summary>
/// Validator for onboarding new employees.
/// </summary>
public sealed class OnboardEmployeeCommandValidator : AbstractValidator<OnboardEmployeeCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public OnboardEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeNumber)
            .NotEmpty().WithMessage("Employee number is required.")
            .MaximumLength(50).WithMessage("Employee number must be 50 characters or less.");

        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required.")
            .MaximumLength(100).WithMessage("Legal name must be 100 characters or less.");

        RuleFor(x => x.WorkEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.WorkEmail))
            .WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email must be 150 characters or less.");

        RuleFor(x => x.HiredOn)
            .NotEmpty().WithMessage("Hired date is required.");
    }
}
