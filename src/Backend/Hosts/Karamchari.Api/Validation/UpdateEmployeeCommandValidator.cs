using FluentValidation;
using Karamchari.HR.Contracts.Employees;

namespace Karamchari.Api.Validation;

/// <summary>
/// Validator for updating employee details.
/// </summary>
public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required.")
            .MaximumLength(100).WithMessage("Legal name must be 100 characters or less.");

        RuleFor(x => x.WorkEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.WorkEmail))
            .WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email must be 150 characters or less.");
    }
}
