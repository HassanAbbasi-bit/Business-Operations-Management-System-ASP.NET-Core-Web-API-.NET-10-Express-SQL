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

## Phase 1 — Requirements and domain model

- [x] Define role permissions and employee/manager relationship rules.
- [x] Finalize entities, enums, relationships, required fields, and delete behavior.
- [x] Decide how an authenticated user maps to an employee profile.
- [x] Define leave-day counting and balance rules.
- [x] Define expense approval rules and task status transitions.
- [x] Review `DATABASE_DESIGN.md`.
- [x] Implement domain entities, enums, value objects, and invariants in `BusinessOperations.Domain`.
- [x] Implement unit tests in `BusinessOperations.UnitTests` (25 tests passing).

## Current milestone: Phase 2 — Persistence foundation

- [ ] Select compatible EF Core 10 packages for `Infrastructure` and `API`.
- [ ] Configure SQL Server connection string (`Server=.\\SQLEXPRESS;Database=BusinessOperationsDb;Trusted_Connection=True;TrustServerCertificate=True`).
- [ ] Implement `ApplicationDbContext` and Fluent API entity configurations.
- [ ] Create initial EF Core migration and generate database.
- [ ] Verify schema and migration reproducibility.

## Upcoming milestones

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

Phase 2 Persistence: Add EF Core 10 packages (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`) and build `ApplicationDbContext` with Fluent API configurations for all entities.

## Completion rule

Only mark an item `[x]` when there is evidence: command output, passing tests, a verified file/configuration, or a confirmed manual test. Record that evidence in `DEVELOPMENT_LOG.md`.
