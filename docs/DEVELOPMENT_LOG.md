# Development Log

Keep entries newest first. Record what changed, files touched, commands/tests run, results, and the next action. Avoid recording credentials or secrets.

## 2026-10-13 — Phase 1: Domain modeling and requirements finalized
 
**Status:** Verified complete
 
**Changes**
- Created pure domain enums: `UserRole`, `LeaveStatus`, `TaskItemStatus`, `TaskPriority`, `ExpenseStatus`, `ExpenseCategory` in `BusinessOperations.Domain/Enums/`.
- Created `BaseEntity` in `BusinessOperations.Domain/Common/` with `Id` (Guid), `CreatedAtUtc`, `UpdatedAtUtc`, and update tracking.
- Created `DomainException` in `BusinessOperations.Domain/Exceptions/` to guard against business invariant violations.
- Implemented rich domain entities in `BusinessOperations.Domain/Entities/`:
  - `User`: Email validation/normalization, password hashing encapsulation, role management, activation states.
  - `Department`: Name, description, manager assignment.
  - `Employee`: Names, department association, manager hierarchy, user account linkage, leave balance adjustments, and self-reporting prevention.
  - `LeaveRequest`: Date interval validation, inclusive days calculation, status lifecycle (`Pending` -> `Approved` / `Rejected` / `Cancelled`), self-approval prevention, rejection reason requirement.
  - `TaskItem`: Title, assignee, creator, priority, status lifecycle (`Todo` -> `InProgress` -> `InReview` -> `Done`), cancellation rules, completion tracking.
  - `Expense`: Category, positive amount validation, future date restriction, currency code formatting, status lifecycle, self-approval prevention.
- Added comprehensive unit tests in `BusinessOperations.UnitTests/Domain/` verifying all invariants:
  - `LeaveRequestTests.cs`: 8 tests
  - `ExpenseTests.cs`: 6 tests
  - `TaskItemTests.cs`: 4 tests
  - `EmployeeTests.cs`: 5 tests
- Finalized `DATABASE_DESIGN.md` with explicit column types, foreign key behaviors, and index definitions.
 
**Verification**
- Commands run:
  - `dotnet test`: 25 passed in `BusinessOperations.UnitTests`, 1 passed in `BusinessOperations.IntegrationTests`. Total 26 passing tests.
  - `dotnet build`: 0 Warnings, 0 Errors.
 
**Decisions**
- Implemented custom `User` entity to maintain architectural decoupling from ASP.NET Identity and give full control over JWT claims and password hashing.
- Established self-review guardrails at the domain level (an employee cannot approve their own leave request or expense submission).
 
**Next action**
- Phase 2: Configure EF Core 10, SQL Server (`.\SQLEXPRESS`), `ApplicationDbContext`, entity configurations, and initial database migration.
 
## 2026-10-13 — Phase 0: Environment verification and solution scaffold completed
 
**Status:** Verified complete
 
**Changes**
- Initialized local Git repository and created standard `.gitignore` with `dotnet new gitignore`.
- Confirmed local SQL Server Express service `MSSQL$SQLEXPRESS` is running. Verified connectivity with `sqlcmd -S ".\SQLEXPRESS" -E -Q "SELECT @@VERSION;"` (Microsoft SQL Server 2025 Express, 17.0.1135.8).
- Generated .NET 10 solution `BusinessOperations.slnx` using the new .NET 10 XML solution format.
- Created 6 projects matching agreed architecture:
  - `src/BusinessOperations.API` (ASP.NET Core Web API with controllers and OpenAPI support)
  - `src/BusinessOperations.Application` (Class library)
  - `src/BusinessOperations.Domain` (Class library)
  - `src/BusinessOperations.Infrastructure` (Class library)
  - `tests/BusinessOperations.UnitTests` (xUnit)
  - `tests/BusinessOperations.IntegrationTests` (xUnit)
- Configured project references strictly following the Clean Architecture dependency graph:
  - API -> Application, Infrastructure
  - Infrastructure -> Application, Domain
  - Application -> Domain
  - UnitTests -> Application, Domain
  - IntegrationTests -> API
- Removed empty template placeholder classes (`Class1.cs`).
 
**Verification**
- Commands run:
  - `dotnet restore`: All projects restored successfully.
  - `dotnet build`: 0 Warnings, 0 Errors.
  - `dotnet test`: 2 test projects passed (UnitTests: 1 passed; IntegrationTests: 1 passed).
 
**Decisions**
- .NET 10 SDK defaults to the modern XML solution format (`.slnx`). Selected `BusinessOperations.slnx` as the primary solution file and removed the empty legacy placeholder `Business Operations Management System.slnx` to prevent MSBuild ambiguity errors.
 
**Next action**
- Phase 1: Finalize domain model entities, enums, rules, and value objects in `BusinessOperations.Domain`.
 
## 2026-10-13 — Project documentation baseline

**Status:** Documentation drafted; not yet confirmed committed to the actual project repository.

### Confirmed
- .NET SDK is `10.0.401`.
- `sqlcmd` is installed, version `15.0.1300.359`.
- Project goal and initial stack agreed.
- Solution/project names and dependency direction agreed.

### Not yet verified
- Whether the solution scaffold commands were run.
- Whether `dotnet restore`, `dotnet build`, or `dotnet test` pass.
- SQL Server instance connectivity and database creation.
- NuGet package versions, migrations, endpoints, and application behavior.

### Next action
Create the solution scaffold, add references, and run restore/build/test. Record actual output and update `TASK_CHECKLIST.md` and `PROJECT_CONTEXT.md`.

## Entry template

### YYYY-MM-DD — Short milestone title

**Status:** In progress / Verified complete / Blocked

**Changes**
- List the files and behavior changed.

**Verification**
- Commands run:
- Test results:
- Manual/API checks:
- Known failures:

**Decisions**
- Record decisions and the reason.

**Next action**
- One concrete next step.
