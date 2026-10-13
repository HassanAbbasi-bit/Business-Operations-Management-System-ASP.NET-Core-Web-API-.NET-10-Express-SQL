# Development Log

Keep entries newest first. Record what changed, files touched, commands/tests run, results, and the next action. Avoid recording credentials or secrets.

## 2026-10-13 — Phase 7: Task management workflow verified

**Status:** Verified complete

**Changes**
- Implemented `ITaskItemRepository` and `TaskItemRepository` in `BusinessOperations.Infrastructure/Repositories/TaskItemRepository.cs` with eager loading, filtering (status, priority, assignee, creator), and pagination.
- Created task DTOs (`CreateTaskItemDto`, `UpdateTaskItemDto`, `TaskItemDto`) in `BusinessOperations.Application/DTOs/Tasks/`.
- Implemented FluentValidation validators (`CreateTaskItemValidator`, `UpdateTaskItemValidator`).
- Implemented `ITaskItemService` and `TaskItemService` in `BusinessOperations.Application/Services/`:
  - Enforces required creator profile, assignee verification, and title constraints.
  - Implements state machine workflow: `StartProgressAsync` (`Todo` -> `InProgress`), `SubmitForReviewAsync` (`InProgress` -> `InReview`), `RequestChangesAsync` (`InReview` -> `InProgress`), `CompleteAsync` (non-cancelled -> `Done`), `CancelAsync` (non-done -> `Cancelled`).
- Implemented `TasksController` in `BusinessOperations.API/Controllers/TasksController.cs`:
  - `GET /api/tasks`: Paginated list with filtering by status, priority, assignee, creator.
  - `GET /api/tasks/{id}`: Single task view.
  - `POST /api/tasks`: Creates a new task.
  - `PUT /api/tasks/{id}`: Updates task details, priority, and assignee.
  - `POST /api/tasks/{id}/start`: Transitions task from Todo to InProgress.
  - `POST /api/tasks/{id}/review`: Transitions task from InProgress to InReview.
  - `POST /api/tasks/{id}/request-changes`: Transitions task from InReview back to InProgress.
  - `POST /api/tasks/{id}/complete`: Marks task as Done.
  - `POST /api/tasks/{id}/cancel`: Cancels task.
- Implemented integration tests in `tests/BusinessOperations.IntegrationTests/Controllers/TasksEndpointTests.cs` (7 tests).

**Verification**
- Commands run:
  - `dotnet test`: 73 tests passing (25 unit tests in `BusinessOperations.UnitTests`, 48 integration tests in `BusinessOperations.IntegrationTests`).
  - `dotnet build`: 0 Warnings, 0 Errors.

**Decisions**
- Implemented strict state machine transition validation in the Domain model and Application service, ensuring invalid out-of-order transitions reject requests with HTTP 400 ProblemDetails.

**Next action**
- Phase 8: Implement Expense Management workflow (submission, amounts, category, audit fields, reviewer self-approval prevention, filtering/pagination).

## 2026-10-13 — Phase 6: Leave management workflow verified

**Status:** Verified complete

**Changes**
- Implemented `ILeaveRequestRepository` and `LeaveRequestRepository` in `BusinessOperations.Infrastructure/Repositories/LeaveRequestRepository.cs` with eager loading, filtering, paging, and date overlap detection (`HasActiveOverlappingRequestAsync`).
- Created leave request DTOs (`CreateLeaveRequestDto`, `LeaveRequestDto`, `ReviewLeaveRequestDto`) in `BusinessOperations.Application/DTOs/LeaveRequests/`.
- Implemented `CreateLeaveRequestValidator` using FluentValidation checking date validity, interval constraints, and required reason.
- Implemented `ILeaveRequestService` and `LeaveRequestService` in `BusinessOperations.Application/Services/`:
  - Enforces positive day counts and checks annual leave balance against requested duration.
  - Detects overlapping active (`Pending` or `Approved`) requests.
  - Connects caller identity to their employee profile.
  - Deducts balance upon manager approval (`employee.DeductLeaveDays(daysCount)`).
  - Enforces self-approval prevention at both application service and domain layers.
  - Requires explicit review reason upon rejection.
  - Allows cancellation only for requests in `Pending` state by their owner.
- Implemented `LeaveRequestsController` in `BusinessOperations.API/Controllers/LeaveRequestsController.cs`:
  - `POST /api/leaverequests`: Submits a new leave request.
  - `GET /api/leaverequests/my`: Retrieves caller's personal leave requests.
  - `GET /api/leaverequests`: Paged search for Managers and Admins with status and department filtering.
  - `GET /api/leaverequests/{id}`: Submitter, Manager, or Admin profile viewing.
  - `POST /api/leaverequests/{id}/approve`: Approves request and deducts leave balance; prevents self-approval.
  - `POST /api/leaverequests/{id}/reject`: Rejects request with audit notes; prevents self-review.
  - `POST /api/leaverequests/{id}/cancel`: Submitter cancels pending request.
- Implemented integration tests in `tests/BusinessOperations.IntegrationTests/Controllers/LeaveRequestsEndpointTests.cs` (11 tests).

**Verification**
- Commands run:
  - `dotnet test`: 66 tests passing (25 unit tests in `BusinessOperations.UnitTests`, 41 integration tests in `BusinessOperations.IntegrationTests`).
  - `dotnet build`: 0 Warnings, 0 Errors.

**Decisions**
- Implemented leave balance deduction on approval rather than submission: Days remain in the balance while in `Pending` review, but overlap checks prevent overbooking dates. Upon approval, `employee.DeductLeaveDays` decrements the official leave balance.
- Enforced rejection reason requirement: Rejection without documented reason is rejected with HTTP 400 ProblemDetails.

**Next action**
- Phase 7: Implement Task Management workflow (creation, assignment, status state machine transitions `Todo` -> `InProgress` -> `InReview` -> `Done`, priority, due date, access control).

## 2026-10-13 — Phase 5: Departments and employee profiles verified

**Status:** Verified complete

**Changes**
- Implemented `IDepartmentRepository` and `DepartmentRepository` in `BusinessOperations.Infrastructure/Repositories/DepartmentRepository.cs` with EF Core queries, eager loading, and duplicate checks.
- Extended `IEmployeeRepository` and implemented `EmployeeRepository` with `GetPagedAsync` (search across first name, last name, email; department filtering; pagination) and `GetAllAsync`.
- Created department DTOs (`DepartmentDto`, `CreateDepartmentDto`, `UpdateDepartmentDto`) and employee DTOs (`EmployeeDto`, `CreateEmployeeDto`, `UpdateEmployeeDto`) in `BusinessOperations.Application/DTOs/`.
- Implemented FluentValidation validators (`CreateDepartmentValidator`, `UpdateDepartmentValidator`, `CreateEmployeeValidator`, `UpdateEmployeeValidator`).
- Implemented `DepartmentService` and `EmployeeService` in `BusinessOperations.Application/Services/` enforcing unique department names, valid department relationships, unique employee emails, and leave balance adjustment safeguards.
- Added `SetLeaveBalance(int days)` domain invariant method to `Employee` entity.
- Implemented `DepartmentsController` in `BusinessOperations.API/Controllers/DepartmentsController.cs`:
  - `GET /api/departments`: Accessible to all authenticated users.
  - `GET /api/departments/{id}`: Returns 200 OK or 404 NotFound.
  - `POST /api/departments`: Admin only; returns 201 Created.
  - `PUT /api/departments/{id}`: Admin only; returns 200 OK.
- Implemented `EmployeesController` in `BusinessOperations.API/Controllers/EmployeesController.cs`:
  - `GET /api/employees`: Paginated search with department filter; Manager or Admin only.
  - `GET /api/employees/{id}`: Manager and Admin can view any employee; regular employees can only view their own profile (403 Forbidden for others).
  - `POST /api/employees`: Admin only; returns 201 Created.
  - `PUT /api/employees/{id}`: Admins can update all fields; non-admins can only update their own profile and are prevented from tampering with department or leave balance (403 Forbidden when attempting to update others).
- Created comprehensive integration test suites:
  - `DepartmentsEndpointTests.cs` (7 tests covering unauthenticated, read, create, duplicate rejection, forbidden role, and update).
  - `EmployeesEndpointTests.cs` (10 tests covering paging, filtering, unauthorized, role barriers, self vs other profile visibility, and tamper protection).

**Verification**
- Commands run:
  - `dotnet test`: 55 tests passing (25 unit tests in `BusinessOperations.UnitTests`, 30 integration tests in `BusinessOperations.IntegrationTests`).
  - `dotnet build`: 0 Warnings, 0 Errors.

**Decisions**
- Implemented tamper protection in `EmployeesController.Update`: when a non-admin updates their own profile, fields like `DepartmentId` and `AnnualLeaveBalanceDays` are locked to their existing values so malicious payload tampering is silently neutralized.
- Integrated real registered users in integration test fixtures to ensure relational foreign key constraints between `Employees.UserId` and `Users.Id` in SQL Server are verified.

**Next action**
- Phase 6: Implement Leave Management workflow (submission, date validation, overlap prevention, balance deductions, manager approval/rejection lifecycle, self-approval prevention).

## 2026-10-13 — Phase 4: Authentication and authorization verified
 
**Status:** Verified complete
 
**Changes**
- Implemented `IPasswordHasher` in `BusinessOperations.Infrastructure/Services/PasswordHasher.cs` using PBKDF2-HMAC-SHA256 with 100,000 iterations and 128-bit cryptographic salt.
- Implemented `IJwtTokenGenerator` in `BusinessOperations.Infrastructure/Services/JwtTokenGenerator.cs` generating signed JWT tokens with Subject, Email, Role, EmployeeId, and unique Jti claims.
- Implemented `IUserRepository`, `IEmployeeRepository`, and `IUnitOfWork` in `BusinessOperations.Infrastructure/Repositories/`.
- Created authentication DTOs (`RegisterRequestDto`, `LoginRequestDto`, `AuthResponseDto`, `CurrentUserDto`) in `BusinessOperations.Application/DTOs/Auth/`.
- Implemented strict input validation with `RegisterRequestValidator` and `LoginRequestValidator` using FluentValidation.
- Implemented `AuthService` in `BusinessOperations.Application/Services/AuthService.cs` handling registration, credential verification, duplicate prevention, and token issuance.
- Configured ASP.NET Core JWT Bearer authentication and role-based policies (`RequireAdmin`, `RequireManager`, `RequireEmployee`) in `BusinessOperations.API/Program.cs`.
- Implemented `AuthController` (`POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`, `GET /api/auth/admin-only`, `GET /api/auth/manager-only`).
- Added comprehensive unit tests (`PasswordHasherTests.cs`) and integration tests (`AuthEndpointTests.cs`).
 
**Verification**
- Commands run:
  - `dotnet test`: 38 tests passing (25 unit tests + 13 integration tests).
  - Integration tests verified:
    - User registration returns HTTP 201 Created and valid JWT token.
    - Duplicate email registration returns HTTP 400 Bad Request.
    - Login with valid credentials returns HTTP 200 OK and token.
    - Login with wrong password returns HTTP 400 Bad Request.
    - Unauthenticated `/api/auth/me` returns HTTP 401 Unauthorized.
    - Authenticated `/api/auth/me` returns HTTP 200 OK with correct profile.
    - Regular employee accessing `/api/auth/admin-only` returns HTTP 403 Forbidden.
    - Regular employee accessing `/api/auth/manager-only` returns HTTP 403 Forbidden.
  - `dotnet build`: 0 Warnings, 0 Errors.
 
**Decisions**
- Enforced password complexity rules (minimum 8 characters, uppercase, lowercase, digit, special character).
- Enforced strict server-side role authorization; public registration assigns `UserRole.Employee` only.
 
**Next action**
- Phase 5: Implement department and employee profile CRUD, pagination, filtering, and role scoping.
 
## 2026-10-13 — Phase 3: API foundation, global exception handling, and health check verified
 
**Status:** Verified complete
 
**Changes**
- Created `ExceptionHandlingMiddleware` in `BusinessOperations.API/Middleware/`:
  - Catches `DomainException` and maps to HTTP 400 Bad Request with RFC 7807 `ProblemDetails`.
  - Catches unhandled exceptions, performs structured logging with `ILogger`, and returns HTTP 500 without leaking stack traces.
- Created generic `PagedResult<T>` in `BusinessOperations.Application/Common/Models/` for consistent collection responses.
- Removed template `WeatherForecast` controller and model.
- Created `HealthController` (`/api/health`) in `BusinessOperations.API/Controllers/` with live SQL Server database check via `CanConnectAsync`.
- Exposed `public partial class Program { }` to enable in-memory integration testing with `WebApplicationFactory`.
- Created live integration test `HealthEndpointTests` in `BusinessOperations.IntegrationTests/Controllers/`.
 
**Verification**
- Commands run:
  - `dotnet test`: 26 tests passing (25 unit tests + 1 live integration test).
  - Health check endpoint integration test against `.\SQLEXPRESS` returned HTTP 200 OK with `{"status":"Healthy","database":"Connected"}`.
  - `dotnet build`: 0 Warnings, 0 Errors.
 
**Decisions**
- Implemented standard RFC 7807 `ProblemDetails` for centralized error response formatting.
- Configured health endpoint to check real database connection readiness.
 
**Next action**
- Phase 4: Implement authentication, password hashing, JWT bearer tokens, registration/login endpoints, and role-based authorization policies.
 
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
