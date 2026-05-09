# Branching Strategy & Protection Rules

## 1. Main Branch is Sacred
The `main` branch represents the deployable, production-ready state of the Karamchari platform. It must ALWAYS build successfully and pass all tests.

## 2. Protection Rules
Direct pushes to `main` are **strictly prohibited**. All changes must flow through Pull Requests (PRs).

**Mandatory PR Gates:**
- **Build & Analyzers:** The `validate` CI workflow must pass (Zero Warnings Policy).
- **Formatting:** `dotnet format` must report zero violations.
- **Security:** Dependency audit (`dotnet list package --vulnerable`) must pass.
- **Review:** At least 1 formal code review approval is required from a code owner.
- **No Bypass:** Administrators cannot bypass branch protection rules.

## 3. Workflow
1. Branch off `main` using the format `feature/your-feature`, `bugfix/issue-description`, or `chore/update-description`.
2. Commit locally, ensuring the `.githooks/pre-commit` script passes.
3. Open a Pull Request against `main`.
4. Address all CI failures and review comments.
5. Merge using **Squash and Merge** to maintain a linear, readable history in `main`.
