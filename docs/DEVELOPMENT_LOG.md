# Development Log

Keep entries newest first. Record what changed, files touched, commands/tests run, results, and the next action. Avoid recording credentials or secrets.

## 2026-10-13 — Phase 2: Persistence foundation completed and database migrated
 
**Status:** Verified complete
 
**Changes**
- Installed EF Core 10 packages:
  - `Microsoft.EntityFrameworkCore.SqlServer` 10.0.12 in `BusinessOperations.Infrastructure`.
  - `Microsoft.EntityFrameworkCore.Design` 10.0.12 in `BusinessOperations.API`.
- Installed local dotnet tool manifest and `dotnet-ef` tool (version 10.0.12).
- Implemented `ApplicationDbContext` and Fluent API entity configurations:
  - `UserConfiguration.cs`: Unique index on email, length constraints, 1-to-0..1 Employee relationship.
  - `DepartmentConfiguration.cs`: Unique index on department name, Restrict delete behavior on employees.
  - `EmployeeConfiguration.cs`: Self-referencing manager hierarchy, unique email, unique filtered index on `UserId`, Restrict delete behavior.
  - `LeaveRequestConfiguration.cs`: Date types, status conversions, composite indexes, Restrict delete behavior.
  - `TaskItemConfiguration.cs`: Priority/status conversions, SetNull on assigned employee, Restrict on creator.
  - `ExpenseConfiguration.cs`: Precision 18,2, ISO currency code, date types, Restrict delete behavior.
- Configured SQL Server connection string in `appsettings.json` and `appsettings.Development.json` targeting `.\SQLEXPRESS` (`BusinessOperationsDb`).
- Registered `AddInfrastructureServices` in `Program.cs`.
- Generated initial EF Core migration `InitialCreate`.
- Applied migration to `.\SQLEXPRESS` creating `BusinessOperationsDb` and all 7 tables with foreign keys and indexes.
 
**Verification**
- Commands run:
  - `dotnet ef dbcontext info`: Successfully resolved `ApplicationDbContext` on `.\SQLEXPRESS`.
  - `dotnet ef migrations add InitialCreate`: Migration generated in `BusinessOperations.Infrastructure/Migrations/`.
  - `dotnet ef database update`: Successfully executed DDL against SQL Server Express.
  - `sqlcmd -S ".\SQLEXPRESS" -d "BusinessOperationsDb" -E -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';"`: Confirmed 7 tables (`__EFMigrationsHistory`, `Departments`, `Users`, `Employees`, `Expenses`, `LeaveRequests`, `TaskItems`).
  - `dotnet test`: 26 tests passing.
  - `dotnet build`: 0 Warnings, 0 Errors.
 
**Decisions**
- Configured delete behavior to `DeleteBehavior.Restrict` for organizational entities and audit records to prevent accidental cascades.
- Used filtered unique index on `Employee.UserId` (`[UserId] IS NOT NULL`) to support optional user association.
 
**Next action**
- Phase 3: Implement API foundation, global exception handling, ProblemDetails, structured logging, and health endpoints.
 
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
