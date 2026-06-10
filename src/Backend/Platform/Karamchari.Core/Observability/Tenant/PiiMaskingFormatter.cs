// -----------------------------------------------------------------------
// <copyright file="PiiMaskingFormatter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Karamchari.Core.Observability.Tenant;

/// <summary>
/// PII (Personally Identifiable Information) masking formatter for secure logging.
/// Masks email addresses, phone numbers, employee IDs, salary data, and other
/// sensitive information in log output according to configurable policies.
/// </summary>
public sealed class PiiMaskingFormatter
{
    private const string MaskValue = "[REDACTED]";

    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new(
        @"\b(\d{3}[-.]?\d{3}[-.]?\d{4}|\(\d{3}\)\s*\d{3}[-.]?\d{4}|\+\d{1,3}[-.\s]?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4})\b",
        RegexOptions.Compiled);

    private static readonly Regex EmployeeIdRegex = new(
        @"\b(EMP-\d{4,}|EMP\d{4,}|EMP-[A-Z0-9]{6,})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SalaryRegex = new(
        @"\b(salary|compensation|pay|wage|bonus)[:\s]*\$?\d{2,}(,\d{3})*(\.\d{2})?\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SsnRegex = new(
        @"\b\d{3}[-]?\d{2}[-]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex CreditCardRegex = new(
        @"\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex BankAccountRegex = new(
        @"\b(account|acct)[:\s]*\d{6,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PasswordRegex = new(
        @"\b(password|passwd|pwd|secret|token|api[_-]?key)[:\s]*[^\s,;]{4,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly PiiMaskingPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the <see cref="PiiMaskingFormatter"/> class
    /// with the default masking policy.
    /// </summary>
    public PiiMaskingFormatter() : this(PiiMaskingPolicy.Strict)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PiiMaskingFormatter"/> class
    /// with a specified masking policy.
    /// </summary>
    /// <param name="policy">The masking policy to apply.</param>
    public PiiMaskingFormatter(PiiMaskingPolicy policy)
    {
        _policy = policy;
    }

    /// <summary>
    /// Masks PII in the given text according to the configured policy.
    /// </summary>
    /// <param name="text">The text containing potential PII.</param>
    /// <returns>The text with PII masked.</returns>
    public string Mask(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return _policy switch
        {
            PiiMaskingPolicy.None => text,
            PiiMaskingPolicy.Basic => MaskBasic(text),
            PiiMaskingPolicy.Strict => MaskStrict(text),
            PiiMaskingPolicy.Maximum => MaskMaximum(text),
            _ => text
        };
    }

    /// <summary>
    /// Masks PII in a log message with key-value pairs.
    /// </summary>
    /// <param name="message">The log message template.</param>
    /// <param name="properties">Key-value properties to check for PII.</param>
    /// <returns>A tuple containing the masked message and properties.</returns>
    public (string MaskedMessage, Dictionary<string, object?> MaskedProperties) MaskLogEntry(
        string message,
        Dictionary<string, object?> properties)
    {
        var maskedMessage = Mask(message);
        var maskedProperties = new Dictionary<string, object?>();

        foreach (var (key, value) in properties)
        {
            if (value is string stringValue)
            {
                maskedProperties[key] = Mask(stringValue);
            }
            else if (value != null)
            {
                maskedProperties[key] = Mask(value.ToString() ?? string.Empty);
            }
            else
            {
                maskedProperties[key] = value;
            }
        }

        return (maskedMessage, maskedProperties);
    }

    private string MaskBasic(string text)
    {
        var result = text;

        result = EmailRegex.Replace(result, MatchEmail);
        result = PhoneRegex.Replace(result, MatchPhone);
        result = EmployeeIdRegex.Replace(result, "***-EMP-***");

        return result;
    }

    private string MaskStrict(string text)
    {
        var result = text;

        result = EmailRegex.Replace(result, MatchEmail);
        result = PhoneRegex.Replace(result, MatchPhone);
        result = EmployeeIdRegex.Replace(result, "***-EMP-***");
        result = SalaryRegex.Replace(result, $"salary: {MaskValue}");
        result = SsnRegex.Replace(result, MatchSsn);
        result = CreditCardRegex.Replace(result, MatchCreditCard);

        return result;
    }

    private string MaskMaximum(string text)
    {
        var result = text;

        result = EmailRegex.Replace(result, MatchEmail);
        result = PhoneRegex.Replace(result, MatchPhone);
        result = EmployeeIdRegex.Replace(result, "***-EMP-***");
        result = SalaryRegex.Replace(result, MaskValue);
        result = SsnRegex.Replace(result, MatchSsn);
        result = CreditCardRegex.Replace(result, MatchCreditCard);
        result = BankAccountRegex.Replace(result, $"{MaskValue}");
        result = PasswordRegex.Replace(result, $"$1: {MaskValue}");

        return result;
    }

    private static string MatchPhone(Match match)
    {
        var phone = match.Value;
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length >= 4
            ? $"***-***-{digits[^4..]}"
            : "***-***-XXXX";
    }

    private static string MatchSsn(Match match)
    {
        var ssn = match.Value;
        var digits = new string(ssn.Where(char.IsDigit).ToArray());
        return digits.Length >= 4
            ? $"***-**-{digits[^4..]}"
            : "***-**-XXXX";
    }

    private static string MatchCreditCard(Match match)
    {
        var cc = match.Value;
        var digits = new string(cc.Where(char.IsDigit).ToArray());
        return digits.Length >= 4
            ? $"****-****-****-{digits[^4..]}"
            : "****-****-****-XXXX";
    }

    private static string MatchEmail(Match match)
    {
        var email = match.Value;
        var atIndex = email.IndexOf('@');

        if (atIndex <= 0)
        {
            return "u***@example.com";
        }

        var username = email[..atIndex];
        var domain = email[(atIndex + 1)..];

        var maskedUsername = username.Length > 2
            ? $"{username[0]}***{username[^1]}"
            : "u***";

        var domainParts = domain.Split('.');
        var maskedDomain = domainParts.Length > 1
            ? $"***.{domainParts[^1]}"
            : "***.com";

        return $"{maskedUsername}@{maskedDomain}";
    }

    /// <summary>
    /// Creates a masked version of sensitive data for display in logs.
    /// </summary>
    /// <param name="value">The sensitive value.</param>
    /// <param name="dataType">Type of sensitive data.</param>
    /// <returns>The masked value.</returns>
    public static string CreateMaskedDisplay(string value, SensitiveDataType dataType)
    {
        return dataType switch
        {
            SensitiveDataType.Email => MaskEmail(value),
            SensitiveDataType.Phone => MaskPhone(value),
            SensitiveDataType.EmployeeId => "***-EMP-***",
            SensitiveDataType.Salary => MaskValue,
            SensitiveDataType.Ssn => "***-**-XXXX",
            SensitiveDataType.CreditCard => "****-****-****-XXXX",
            SensitiveDataType.BankAccount => "****XXXX",
            SensitiveDataType.Password => "********",
            _ => MaskValue
        };
    }

    private static string MaskEmail(string email)
    {
        return EmailRegex.Replace(email, MatchEmail);
    }

    private static string MaskPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        return digits.Length >= 4
            ? $"***-***-{digits[^4..]}"
            : "***-***-XXXX";
    }
}

/// <summary>
/// Policy level for PII masking.
/// </summary>
public enum PiiMaskingPolicy
{
    /// <summary>No masking applied.</summary>
    None,

    /// <summary>Basic masking for email, phone, employee ID.</summary>
    Basic,

    /// <summary>Strict masking including salary, SSN, credit cards.</summary>
    Strict,

    /// <summary>Maximum masking including passwords and bank accounts.</summary>
    Maximum
}

/// <summary>
/// Types of sensitive data that can be masked.
/// </summary>
public enum SensitiveDataType
{
    /// <summary>Email address.</summary>
    Email,

    /// <summary>Phone number.</summary>
    Phone,

    /// <summary>Employee identifier.</summary>
    EmployeeId,

    /// <summary>Salary or compensation.</summary>
    Salary,

    /// <summary>Social Security Number.</summary>
    Ssn,

    /// <summary>Credit card number.</summary>
    CreditCard,

    /// <summary>Bank account number.</summary>
    BankAccount,

    /// <summary>Password or secret.</summary>
    Password
}
