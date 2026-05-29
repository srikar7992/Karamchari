# Commit Conventions

## 1. Format
We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification.
Format: `<type>(<scope>): <short summary>`

## 2. Allowed Types
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Changes that do not affect the meaning of the code (white-space, formatting, missing semi-colons, etc)
- `refactor`: A code change that neither fixes a bug nor adds a feature
- `perf`: A code change that improves performance
- `test`: Adding missing tests or correcting existing tests
- `build`: Changes that affect the build system or external dependencies
- `ci`: Changes to our CI configuration files and scripts
- `chore`: Other changes that don't modify src or test files

## 3. Scope
The scope should refer to the bounded context or module affected (e.g., `payroll`, `attendance`, `core`, `governance`).

Example:
`feat(attendance): add confidence scoring to check-in flow`
`fix(payroll): resolve rounding issue in arrear calculation`

## 4. Subject Line
- Use the imperative, present tense: "change" not "changed" nor "changes".
- Don't capitalize the first letter.
- No dot (.) at the end.
