## Commit Message Convention

This project follows [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) to maintain a consistent, readable Git history and support automated changelog generation and semantic versioning.

### Commit Message Format

```text
<type>(optional scope): <description>
```

- **type**: The category of change.
- **scope** *(optional)*: The affected module or component.
- **description**: A short summary written in imperative mood.

### Commit Types

| Type | Description |
|---|---|
| `feat` | Introduces a new feature or functionality. |
| `fix` | Fixes a bug. |
| `refactor` | Restructures code without changing its external behavior. |
| `perf` | Improves application performance. |
| `docs` | Adds or updates documentation. |
| `test` | Adds or updates tests. |
| `style` | Applies formatting changes without affecting code behavior. |
| `chore` | Performs routine maintenance tasks. |
| `build` | Changes build systems or dependencies. |
| `ci` | Changes CI/CD configuration. |
| `revert` | Reverts a previous commit. |

### Examples

```bash
feat: add admin dashboard
feat(auth): implement Google OAuth authentication
feat(scheduler): add database-driven scheduling

fix: resolve incorrect date formatting
fix(auth): handle expired authentication tokens

refactor: simplify service layer
refactor(scheduler): improve execution pipeline

perf: optimize database queries

docs: update installation instructions

test: add authentication tests

chore: update dependencies
build: update Docker configuration
ci: add automated testing workflow
```

### Breaking Changes

Use `!` after the type or scope to indicate a breaking change.

```bash
feat!: redesign authentication API
```

Alternatively, include a `BREAKING CHANGE:` footer in the commit message.

```text
feat(auth): redesign authentication API

BREAKING CHANGE: The authentication endpoint now requires a different request payload.
```

### Semantic Versioning

Conventional Commits can be used to determine version increments:

| Change | Version Increment | Example |
|---|---|---|
| `fix` | PATCH | `1.0.0` → `1.0.1` |
| `feat` | MINOR | `1.0.0` → `1.1.0` |
| Breaking change (`!`) | MAJOR | `1.0.0` → `2.0.0` |

### Guidelines

1. Use lowercase for commit types and descriptions.
2. Keep descriptions concise and meaningful.
3. Use the imperative mood (e.g. `add`, `fix`, `update`).
4. Use scopes when they help identify the affected module.
5. Avoid vague messages such as `update code`, `changes`, or `fix stuff`.
6. Each commit should represent a logical and preferably self-contained change.
