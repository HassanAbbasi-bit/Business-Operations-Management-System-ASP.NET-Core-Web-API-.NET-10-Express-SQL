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

## Phase 2 — Persistence foundation

- [x] Select compatible EF Core 10 packages for `Infrastructure` and `API` (`Microsoft.EntityFrameworkCore.SqlServer` 10.0.12, `Microsoft.EntityFrameworkCore.Design` 10.0.12).
- [x] Configure SQL Server connection string (`Server=.\\SQLEXPRESS;Database=BusinessOperationsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`).
- [x] Implement `ApplicationDbContext` and Fluent API entity configurations.
- [x] Create initial EF Core migration (`InitialCreate`) and generate database.
- [x] Verify schema and migration reproducibility against `.\SQLEXPRESS`.

## Phase 3 — API foundation

- [x] Add consistent API response conventions and ProblemDetails middleware.
- [x] Configure global exception handling middleware with structured logging (`ExceptionHandlingMiddleware`).
- [x] Configure OpenAPI / Swagger with custom document metadata.
- [x] Add API health check endpoint (`/api/health`) and verify live DB connection.
- [x] Validate environment configuration and secrets handling.

## Phase 4 — Authentication and authorization

- [x] Select secure password hashing service (`PasswordHasher` with PBKDF2-HMAC-SHA256).
- [x] Configure JWT options (Issuer, Audience, SecretKey, Expiration) and JWT Bearer authentication.
- [x] Implement registration and login DTOs and FluentValidation validators.
- [x] Implement `IAuthService` and `AuthController` (`/api/auth/register`, `/api/auth/login`, `/api/auth/me`).
- [x] Implement role-based authorization policies (`RequireAdmin`, `RequireManager`, `RequireEmployee`).
- [x] Test valid/invalid login, token verification, and role authorization boundaries (`403 Forbidden`).

## Phase 5 — Departments and employee profiles

- [x] Implement department repository, DTOs, and endpoints (`GET /api/departments`, `POST /api/departments`, `PUT /api/departments/{id}`).
- [x] Implement employee profile repository, DTOs, and endpoints (`GET /api/employees`, `GET /api/employees/{id}`, `POST /api/employees`, `PUT /api/employees/{id}`).
- [x] Add pagination and search filtering (`PagedResult<T>`).
- [x] Enforce role-based access control (Admin can manage; Manager can search/list; Employee can view/edit self only; tamper protection).
- [x] Add unit and integration tests (30 integration tests passing, 25 unit tests passing).

## Phase 6 — Leave management

- [x] Implement leave request repository and DTOs (`ILeaveRequestRepository`, `LeaveRequestRepository`, `CreateLeaveRequestDto`, `LeaveRequestDto`, `ReviewLeaveRequestDto`).
- [x] Submit leave request endpoint with validation (date ranges, overlap prevention, positive day counts).
- [x] Leave balance check and deduction upon approval (`employee.DeductLeaveDays(daysCount)`).
- [x] List own leave requests (`GET /api/leaverequests/my`) and review list (`GET /api/leaverequests` for Manager/Admin).
- [x] Approve/Reject workflow with reviewer auditing and prevent self-approval.
- [x] Add unit and integration tests (41 integration tests passing, 25 unit tests passing).

## Phase 7 — Task management

- [x] Implement task repository, DTOs, and validators (`ITaskItemRepository`, `TaskItemRepository`, `CreateTaskItemDto`, `UpdateTaskItemDto`, `TaskItemDto`).
- [x] Create and assign tasks endpoint (`POST /api/tasks`, `PUT /api/tasks/{id}`).
- [x] List tasks by assignee/status/priority/due date with pagination (`GET /api/tasks`).
- [x] Define and enforce permitted status transitions (`Todo` -> `InProgress` -> `InReview` -> `Done`, cancellation and changes-requested guards).
- [x] Enforce who can create, assign, edit, and complete tasks.
- [x] Add unit and integration tests (48 integration tests passing, 25 unit tests passing).

## Current milestone: Phase 8 — Expense management

- [ ] Implement expense repository, DTOs, and validators.
- [ ] Submit expense endpoint with amount, currency, category, date, and description.
- [ ] Review/approve/reject expenses with audit fields and prevent self-approval.
- [ ] List and filter expenses with pagination.
- [ ] Add unit and integration tests.

## Upcoming milestones

- [ ] Phase 9: dashboard, quality, and integration tests.
- [ ] Phase 10: README, GitHub, deployment readiness.
- [ ] Phase 11: optional frontend.

## Current next action

Phase 8 Expense Management: Implement expense repository, DTOs, service, review/approval workflow, and endpoints with domain validations in `BusinessOperations.Application`, `BusinessOperations.Infrastructure`, and `BusinessOperations.API`.

## Completion rule

Only mark an item `[x]` when there is evidence: command output, passing tests, a verified file/configuration, or a confirmed manual test. Record that evidence in `DEVELOPMENT_LOG.md`.
