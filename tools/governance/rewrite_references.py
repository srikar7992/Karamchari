#!/usr/bin/env python3
"""
Karamchari Repository Governance — Phase 4 Reference Rewriter.

After `git mv` has physically moved all projects to their new locations,
this script updates:
  1. Every <ProjectReference Include="..."> in every .csproj under
     src/Backend/**  and tests/Backend/**
  2. Every project path in src/Backend/Karamchari.sln

Run AFTER git mv operations are complete.
"""

import os
import re
from pathlib import Path

REPO_ROOT = Path("/Users/srikarbojji/Projects/Karamchari")
BACKEND   = REPO_ROOT / "src" / "Backend"
TESTS_DIR = REPO_ROOT / "tests" / "Backend"
SLN       = BACKEND / "Karamchari.sln"

# ─── Canonical mapping: project name → new path relative to BACKEND ──────────
SOURCE_NEW_PATHS: dict[str, str] = {
    # Platform
    "Karamchari.Core":                     "Platform/Karamchari.Core",
    "Karamchari.Core.Contracts":           "Platform/Karamchari.Core.Contracts",
    "Karamchari.Identity":                 "Platform/Karamchari.Identity",
    "Karamchari.Identity.Contracts":       "Platform/Karamchari.Identity.Contracts",
    "Karamchari.Identity.Infrastructure":  "Platform/Karamchari.Identity.Infrastructure",
    # Hosts
    "Karamchari.Api":                      "Hosts/Karamchari.Api",
    "Karamchari.Worker":                   "Hosts/Karamchari.Worker",
    # Modules
    "Karamchari.Billing":                  "Modules/Billing/Karamchari.Billing",
    "Karamchari.Billing.Contracts":        "Modules/Billing/Karamchari.Billing.Contracts",
    "Karamchari.Capability":               "Modules/Capability/Karamchari.Capability",
    "Karamchari.Capability.Contracts":     "Modules/Capability/Karamchari.Capability.Contracts",
    "Karamchari.Compensation":             "Modules/Compensation/Karamchari.Compensation",
    "Karamchari.Compensation.Contracts":   "Modules/Compensation/Karamchari.Compensation.Contracts",
    "Karamchari.DataMigration":            "Modules/DataMigration/Karamchari.DataMigration",
    "Karamchari.DataMigration.Contracts":  "Modules/DataMigration/Karamchari.DataMigration.Contracts",
    "Karamchari.FinancialOps":             "Modules/FinancialOps/Karamchari.FinancialOps",
    "Karamchari.FinancialOps.Contracts":   "Modules/FinancialOps/Karamchari.FinancialOps.Contracts",
    "Karamchari.Forecasting":              "Modules/Forecasting/Karamchari.Forecasting",
    "Karamchari.Governance":               "Modules/Governance/Karamchari.Governance",
    "Karamchari.HR":                       "Modules/HR/Karamchari.HR",
    "Karamchari.Intelligence":             "Modules/Intelligence/Karamchari.Intelligence",
    "Karamchari.Intelligence.Contracts":   "Modules/Intelligence/Karamchari.Intelligence.Contracts",
    "Karamchari.Notifications":            "Modules/Notifications/Karamchari.Notifications",
    "Karamchari.Payroll":                  "Modules/Payroll/Karamchari.Payroll",
    "Karamchari.Payroll.Contracts":        "Modules/Payroll/Karamchari.Payroll.Contracts",
    "Karamchari.Performance":              "Modules/Performance/Karamchari.Performance",
    "Karamchari.Performance.Contracts":    "Modules/Performance/Karamchari.Performance.Contracts",
    "Karamchari.PSA":                      "Modules/PSA/Karamchari.PSA",
    "Karamchari.Recruitment":              "Modules/Recruitment/Karamchari.Recruitment",
    "Karamchari.Recruitment.Contracts":    "Modules/Recruitment/Karamchari.Recruitment.Contracts",
    "Karamchari.TimeAttendance":           "Modules/TimeAttendance/Karamchari.TimeAttendance",
    "Karamchari.TimeAttendance.Contracts": "Modules/TimeAttendance/Karamchari.TimeAttendance.Contracts",
    "Karamchari.Workflow":                 "Modules/Workflow/Karamchari.Workflow",
}

# Test projects that moved from src/Backend/ → tests/Backend/
MOVED_TESTS: dict[str, Path] = {
    "Karamchari.PSA.Tests":             TESTS_DIR / "Karamchari.PSA.Tests",
    "Karamchari.Payroll.Tests":         TESTS_DIR / "Karamchari.Payroll.Tests",
    "Karamchari.TimeAttendance.Tests":  TESTS_DIR / "Karamchari.TimeAttendance.Tests",
}

# ─── Helpers ─────────────────────────────────────────────────────────────────

def to_backslash(p: str) -> str:
    return p.replace("/", "\\")

def new_csproj(proj_name: str) -> Path:
    """Absolute path of the .csproj file after relocation."""
    return BACKEND / SOURCE_NEW_PATHS[proj_name] / f"{proj_name}.csproj"

def make_ref(from_csproj_dir: Path, to_csproj: Path) -> str:
    """Relative backslash path from a project dir to another .csproj."""
    rel = os.path.relpath(to_csproj, from_csproj_dir)
    return to_backslash(rel)

# ─── .csproj updaters ────────────────────────────────────────────────────────

def update_csproj(csproj_path: Path) -> None:
    content = csproj_path.read_text(encoding="utf-8")
    proj_dir = csproj_path.parent
    changed  = False

    def replace_ref(m: re.Match) -> str:
        nonlocal changed
        raw        = m.group(1)
        proj_name  = Path(raw.replace("\\", "/")).stem
        if proj_name in SOURCE_NEW_PATHS:
            new_rel = make_ref(proj_dir, new_csproj(proj_name))
            changed = True
            return f'Include="{new_rel}"'
        return m.group(0)

    new_content = re.sub(r'Include="([^"]+\.csproj)"', replace_ref, content)
    if changed:
        csproj_path.write_text(new_content, encoding="utf-8")
        print(f"  [updated] {csproj_path.relative_to(REPO_ROOT)}")
    else:
        print(f"  [skip]    {csproj_path.relative_to(REPO_ROOT)}")

# ─── .sln updater ────────────────────────────────────────────────────────────

# Map from old project name → new sln-relative path
def _new_sln_path(proj_name: str) -> str:
    if proj_name in SOURCE_NEW_PATHS:
        abs_csproj = new_csproj(proj_name)
    elif proj_name in MOVED_TESTS:
        abs_csproj = MOVED_TESTS[proj_name] / f"{proj_name}.csproj"
    else:
        return ""
    rel = os.path.relpath(abs_csproj, SLN.parent)
    return to_backslash(rel)

def update_sln() -> None:
    content = SLN.read_text(encoding="utf-8")

    # Solution folder GUIDs to add (Platform / Modules / Hosts / tests)
    SF = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}"

    # New solution-folder definitions to inject after the existing "src" folder line
    new_folders = (
        f'Project("{SF}") = "Platform", "Platform", "{{B0000002-0000-0000-0000-000000000002}}"\n'
        f'EndProject\n'
        f'Project("{SF}") = "Modules", "Modules", "{{B0000003-0000-0000-0000-000000000003}}"\n'
        f'EndProject\n'
        f'Project("{SF}") = "Hosts", "Hosts", "{{B0000004-0000-0000-0000-000000000004}}"\n'
        f'EndProject\n'
        f'Project("{SF}") = "tests", "tests", "{{B0000005-0000-0000-0000-000000000005}}"\n'
        f'EndProject\n'
    )

    # ── 1. Update project paths ───────────────────────────────────────────────
    PROJ_RE = re.compile(
        r'(Project\("[^"]+"\) = "[^"]+", ")([^"]+\.csproj)(".*)',
        re.IGNORECASE,
    )

    def replace_proj(m: re.Match) -> str:
        prefix   = m.group(1)
        old_path = m.group(2)
        suffix   = m.group(3)
        norm     = old_path.replace("\\", "/")
        name     = Path(norm).stem
        new_path = _new_sln_path(name)
        if new_path:
            return f"{prefix}{new_path}{suffix}"
        return m.group(0)

    content = PROJ_RE.sub(replace_proj, content)

    # ── 2. Inject new solution folders (after the "src" folder block) ─────────
    src_folder_end = 'Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "src", "src"'
    if new_folders.split("\n")[0] not in content:
        # Find the EndProject line after the "src" folder and inject after it
        content = content.replace(
            src_folder_end,
            new_folders + src_folder_end,
        )

    # ── 3. Rebuild NestedProjects section ─────────────────────────────────────
    # GUIDs for each source project (sourced from the .sln)
    PLATFORM_GUIDS = {
        "{A1000002-0000-0000-0000-000000000002}",  # Core
        "{A1000007-0000-0000-0000-000000000007}",  # Core.Contracts
        "{36345336-F6CB-4BC8-A28D-BBC026E4E84A}",  # Identity
        "{08081E58-5598-4004-84E4-AE5CA2210A45}",  # Identity.Contracts
        "{19067105-2915-46EE-9316-1DF8F2373F61}",  # Identity.Infrastructure
    }
    HOSTS_GUIDS = {
        "{A1000001-0000-0000-0000-000000000001}",  # Api
        # Worker GUID — find from sln
    }
    TESTS_GUIDS = {
        "{A1000005-0000-0000-0000-000000000005}",  # Core.UnitTests
        "{A1000006-0000-0000-0000-000000000006}",  # Core.IntegrationTests
        "{6A9397B0-1C40-4031-ADC0-B0EE77180F68}",  # Api.UnitTests
        "{CF3FDDAF-0E8C-4AC6-8A6B-8389093849D9}",  # Identity.IntegrationTests
        "{EDBCFE0B-D5F1-4644-8BA4-5384AB645703}",  # TenantIsolationCertification
        "{A1000015-0000-0000-0000-000000000015}",  # FinancialChaosTests
        "{79D1BE36-8769-4423-820B-FA5DF7DC9D08}",  # ArchitectureTests
        "{0DCDB855-9516-498D-9503-BC2AE2BF3BBC}",  # DataMigration.Tests
        "{10B1710D-4E46-44AE-AF5B-38590F8B2E5D}",  # TimeAttendance.Tests
        "{A1000009-0000-0000-0000-000000000009}",  # PSA.Tests
        "{A100000C-0000-0000-0000-00000000000C}",  # Payroll.Tests
    }
    # Everything else → Modules folder
    ALL_KNOWN = PLATFORM_GUIDS | HOSTS_GUIDS | TESTS_GUIDS

    # Reconstruct the NestedProjects section
    # Extract all project GUIDs and old parent assignments
    nested_re = re.compile(r"\t\t(\{[0-9A-F-]+\}) = (\{[0-9A-F-]+\})", re.IGNORECASE)
    existing_nesting = dict(nested_re.findall(content))  # child -> parent

    # All project GUIDs present in the sln
    all_proj_guids = re.findall(r', "(\{[0-9A-F-]+\})"$', content, re.MULTILINE)

    def parent_for(guid: str) -> str:
        gu = guid.upper()
        if gu in {g.upper() for g in PLATFORM_GUIDS}:
            return "{B0000002-0000-0000-0000-000000000002}"
        if gu in {g.upper() for g in HOSTS_GUIDS}:
            return "{B0000004-0000-0000-0000-000000000004}"
        if gu in {g.upper() for g in TESTS_GUIDS}:
            return "{B0000005-0000-0000-0000-000000000005}"
        # Modules
        return "{B0000003-0000-0000-0000-000000000003}"

    # Build new NestedProjects block
    new_nested_lines = []
    for guid in all_proj_guids:
        gu = guid.upper()
        # Skip solution folder GUIDs themselves
        if gu in {
            "{B0000001-0000-0000-0000-000000000001}",
            "{B0000002-0000-0000-0000-000000000002}",
            "{B0000003-0000-0000-0000-000000000003}",
            "{B0000004-0000-0000-0000-000000000004}",
            "{B0000005-0000-0000-0000-000000000005}",
        }:
            continue
        parent = parent_for(guid)
        new_nested_lines.append(f"\t\t{guid} = {parent}")

    # Platform, Modules, Hosts folders nested under "src" solution folder
    new_nested_lines += [
        "\t\t{B0000002-0000-0000-0000-000000000002} = {B0000001-0000-0000-0000-000000000001}",
        "\t\t{B0000003-0000-0000-0000-000000000003} = {B0000001-0000-0000-0000-000000000001}",
        "\t\t{B0000004-0000-0000-0000-000000000004} = {B0000001-0000-0000-0000-000000000001}",
    ]

    new_nested_block = (
        "\tGlobalSection(NestedProjects) = preSolution\n"
        + "\n".join(new_nested_lines)
        + "\n\tEndGlobalSection\n"
    )

    # Replace existing NestedProjects section
    content = re.sub(
        r"\tGlobalSection\(NestedProjects\) = preSolution.*?\tEndGlobalSection\n",
        new_nested_block,
        content,
        flags=re.DOTALL,
    )

    SLN.write_text(content, encoding="utf-8")
    print("  [updated] Karamchari.sln")

# ─── Entry point ─────────────────────────────────────────────────────────────

def main() -> None:
    print("=" * 60)
    print("Karamchari Reference Rewriter — Phase 4")
    print("=" * 60)

    # 1. Update all source project .csproj files
    print("\n── Source project .csproj files ──")
    for proj_name, rel_path in SOURCE_NEW_PATHS.items():
        csproj = BACKEND / rel_path / f"{proj_name}.csproj"
        if csproj.exists():
            update_csproj(csproj)
        else:
            print(f"  [MISSING] {csproj.relative_to(REPO_ROOT)}")

    # 2. Update moved test .csproj files (now in tests/Backend/)
    print("\n── Moved test .csproj files ──")
    for proj_name, test_dir in MOVED_TESTS.items():
        csproj = test_dir / f"{proj_name}.csproj"
        if csproj.exists():
            update_csproj(csproj)
        else:
            print(f"  [MISSING] {csproj.relative_to(REPO_ROOT)}")

    # 3. Update all other test project .csproj files
    print("\n── Existing test .csproj files ──")
    for csproj in sorted(TESTS_DIR.rglob("*.csproj")):
        # skip the already-handled moved tests (they're now in TESTS_DIR too)
        update_csproj(csproj)

    # 4. Update .sln
    print("\n── Solution file ──")
    update_sln()

    print("\n✓ Reference rewriter complete.\n")

if __name__ == "__main__":
    main()
