# Working Rules for ChatGPT / AI Coding Assistants

Read this file, `PROJECT_CONTEXT.md`, `TASK_CHECKLIST.md`, and the latest entry in `DEVELOPMENT_LOG.md` before proposing project changes.

## Source-of-truth order

1. Actual source files, `.csproj` files, migrations, tests, and command output.
2. `PROJECT_CONTEXT.md` and `TASK_CHECKLIST.md` for verified status.
3. `DEVELOPMENT_LOG.md` for history and decisions.
4. `PROJECT_PLAN.md`, `ARCHITECTURE.md`, `DATABASE_DESIGN.md`, and `API_CONTRACT.md` for intended future design.
5. Earlier chat messages only where not superseded by the repository.

If documents conflict with actual code or command output, do not silently assume. Explain the mismatch and update the documents after confirming the correct state.

## Before each work session

1. Identify the current milestone and first incomplete task.
2. Check what files and projects actually exist.
3. Ask for missing command output only when necessary; do not pretend to inspect the user's machine.
4. State the specific task being performed and its acceptance criteria.
5. Avoid redoing completed tasks unless verification shows a problem.

## During implementation

- Teach with **What, Why, How, Code, Test** for major concepts.
- Give exact relative file paths and commands.
- Keep changes focused; avoid unrelated refactors.
- Do not invent existing files, command results, package versions, SQL Server instance names, credentials, or deployment settings.
- Do not claim code was run on the user's computer.
- Do not mark a task complete until the user provides evidence or the available tools actually verify it.
- If a task fails, record the failure and next diagnostic action.
- Prefer stable packages compatible with .NET 10.
- Use async APIs and cancellation tokens for I/O where appropriate.
- Keep controllers thin and use DTOs.
- Use EF Core migrations for schema changes and explain migration commands.
- Protect endpoints with server-side authorization and test role boundaries.
- Never store plaintext passwords or commit secrets.
- Never log tokens, passwords, signing keys, or connection strings.
- Use fake records only for tests or explicitly development-only seed data; don't hardcode fake results into production endpoints.
- Avoid introducing new architectural frameworks or infrastructure without explaining the need.

## After each milestone

1. Run the relevant build/tests/API checks, or clearly state which checks the user must run.
2. Summarize changed files and behavior.
3. Report actual test output, including failures.
4. Update `TASK_CHECKLIST.md`.
5. Add a dated entry to `DEVELOPMENT_LOG.md`.
6. Update `PROJECT_CONTEXT.md` if confirmed environment, architecture, or progress facts changed.
7. Give one clear next action.

## Task completion definition

A task is complete only when:
- implementation exists in the intended files,
- it builds,
- relevant tests pass or documented manual checks succeed,
- security/business rules have been considered,
- documentation and progress tracking are updated.

For a purely documentary task, completion means the files exist and their contents have been reviewed.
