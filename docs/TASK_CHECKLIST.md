# Task Checklist

This file is the quick progress board. Update it only when work is actually verified.

Legend: `[ ]` not done, `[-]` in progress, `[x]` complete and verified, `[!]` blocked.

## Current milestone: Phase 1 — Requirements and domain model

- [x] Verify `dotnet --version` (`10.0.401`).
- [x] Verify `dotnet --list-sdks` (`10.0.401`).
- [x] Verify `sqlcmd` executable (`15.0.1300.359`).
- [x] Confirm SQL Server engine/instance can be reached (`.\SQLEXPRESS` running Microsoft SQL Server 2025 Express).
- [x] Create repository working directory and initialize Git.
- [x] Create `BusinessOperations.slnx` (using .NET 10 XML solution format).
- [x] Create API, Application, Domain, Infrastructure projects.
- [x] Create UnitTests and IntegrationTests projects.
- [x] Add projects to solution.
- [x] Add references per architecture.
- [x] Run `dotnet restore`.
- [x] Run `dotnet build`.
- [x] Run baseline `dotnet test`.
- [x] Commit scaffold and project documentation.

## Upcoming milestones

- [-] Phase 1: finalize requirements/domain model.
- [ ] Phase 2: configure EF Core and SQL Server.
- [ ] Phase 3: API foundation and error handling.
- [ ] Phase 4: authentication/authorization.
- [ ] Phase 5: departments and employees.
- [ ] Phase 6: leave workflow.
- [ ] Phase 7: task workflow.
- [ ] Phase 8: expense workflow.
- [ ] Phase 9: dashboard, quality, and integration tests.
- [ ] Phase 10: README, GitHub, deployment readiness.
- [ ] Phase 11: optional frontend.

## Current next action

Phase 1 Domain Modeling: Review and finalize the core domain entities, enums, rules, and value objects in `BusinessOperations.Domain` (Department, Employee, User, Role, LeaveRequest, TaskItem, Expense) before writing EF Core mappings.

## Completion rule

Only mark an item `[x]` when there is evidence: command output, passing tests, a verified file/configuration, or a confirmed manual test. Record that evidence in `DEVELOPMENT_LOG.md`.
