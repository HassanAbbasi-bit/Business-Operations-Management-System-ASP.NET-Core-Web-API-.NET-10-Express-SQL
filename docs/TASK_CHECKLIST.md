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

## Phase 8 — Expense management

- [x] Implement expense repository, DTOs, and validators (`IExpenseRepository`, `ExpenseRepository`, `CreateExpenseDto`, `ExpenseDto`, `ReviewExpenseDto`).
- [x] Submit expense endpoint with amount, currency, category, date, and description (`POST /api/expenses`).
- [x] Review/approve/reject expenses with audit fields and prevent self-approval (`POST /api/expenses/{id}/approve`, `POST /api/expenses/{id}/reject`).
- [x] List and filter expenses with pagination (`GET /api/expenses`, `GET /api/expenses/my`, `POST /api/expenses/{id}/cancel`).
- [x] Add unit and integration tests (59 integration tests passing, 25 unit tests passing).

## Phase 9 — Dashboard and quality

- [x] Add dashboard summary aggregate endpoints based on real database data (`DashboardController`, `DashboardService`).
- [x] Optimize queries and verify async execution and cancellation tokens across all repositories.
- [x] Review validation, error responses, structured logging, and sensitive data safeguards.
- [x] Add end-to-end integration tests for dashboard metrics against verified test data (`DashboardEndpointTests.cs`, 63 integration tests passing, 25 unit tests passing).

## Phase 10 — Documentation and release

- [x] Complete portfolio-grade README setup instructions and architecture documentation.
- [x] Document architecture, database design, endpoints, roles, and configuration.
- [x] Verify `.gitignore` excludes build artifacts and local secrets.
- [x] Document deployment readiness, migration instructions, and API contract.
- [x] All 88 automated tests passing cleanly with 0 errors.

## Phase 11 — Modern React Web Dashboard (Operix)

- [x] Initialized Vite React application in `frontend/`.
- [x] Installed `lucide-react` icons and configured development proxy in `vite.config.js`.
- [x] Configured CORS in `BusinessOperations.API/Program.cs`.
- [x] Implemented API client with JWT authentication, RFC 7807 error parsing, and endpoint methods in `frontend/src/api.js`.
- [x] Created bespoke dark mode design system in `frontend/src/index.css` and `frontend/src/App.css`.
- [x] Built full interactive Operix Command Center in `frontend/src/App.jsx` with Overview, Personnel & Teams, Time Off & Leaves, Task Operations Matrix (Kanban), and Reimbursements.
- [x] Implemented quick 1-click Demo Profiles for Admin, Manager, and Employee.
- [x] Verified production build (`npm run build`) and live local server connectivity.

## Current next action

Full solution complete across Phases 0 through 11. Backend API (88 tests passing) and Frontend Dashboard (Operix) verified and ready for showcase.

## Completion rule

Only mark an item `[x]` when there is evidence: command output, passing tests, a verified file/configuration, or a confirmed manual test. Record that evidence in `DEVELOPMENT_LOG.md`.
