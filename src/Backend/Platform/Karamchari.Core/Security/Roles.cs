// -----------------------------------------------------------------------
// <copyright file="Roles.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Security;

/// <summary>
/// Centralized registry of platform roles.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string ReadOnly = "ReadOnly";
    public const string Recruiter = "Recruiter";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        SuperAdmin,
        Admin,
        Manager,
        Employee,
        ReadOnly,
        Recruiter
    };
}
