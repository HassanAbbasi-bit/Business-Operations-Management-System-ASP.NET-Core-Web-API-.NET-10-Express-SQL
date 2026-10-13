# Project Plan and Delivery Gates

## Delivery principles

- Deliver a working, testable vertical slice at a time.
- Prefer correctness and clear business rules over adding many features quickly.
- Do not start the frontend before the core API is stable.
- Every phase must have a test/verification gate.
- Update `TASK_CHECKLIST.md` and `DEVELOPMENT_LOG.md` when a gate passes.

## Phase 0 — Environment and scaffold

- [ ] Confirm working directory and Git repository.
- [ ] Create solution and six projects with the agreed names.
- [ ] Add projects to the solution.
- [ ] Add project references using `ARCHITECTURE.md`.
- [ ] Run `dotnet restore` and `dotnet build`.
- [ ] Run baseline unit/integration test projects.
- [ ] Add `docs/` files to the repository and commit the baseline.
**Gate:** clean build and baseline tests pass; output is recorded.

## Phase 1 — Requirements and domain model

- [x] Define role permissions and employee/manager relationship rules.
- [x] Finalize entities, enums, relationships, required fields, and delete behavior.
- [x] Decide how an authenticated user maps to an employee profile.
- [x] Define leave-day counting and balance rules.
- [x] Define expense approval rules and task status transitions.
- [x] Review `DATABASE_DESIGN.md`.
**Gate:** domain and database decisions are documented before migrations. (Gate Passed: verified with 25 unit tests).

## Phase 2 — Persistence foundation

- [x] Select compatible EF Core 10 packages (`Microsoft.EntityFrameworkCore.SqlServer` 10.0.12, `Microsoft.EntityFrameworkCore.Design` 10.0.12).
- [x] Configure SQL Server connection string in `appsettings.json` and `appsettings.Development.json` (`Server=.\\SQLEXPRESS;Database=BusinessOperationsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`).
- [x] Add `ApplicationDbContext` and entity configurations using Fluent API.
- [x] Configure indexes, constraints, relationships, and delete behavior.
- [x] Create first migration (`InitialCreate`) and database.
- [x] Verify schema and migration reproducibility (verified with `sqlcmd` against `BusinessOperationsDb`).
**Gate:** app connects to the intended local SQL Server instance and migrations work. (Gate Passed: verified against `.\SQLEXPRESS`).

## Phase 3 — API foundation

- [x] Configure dependency injection and application services (`AddInfrastructureServices`).
- [x] Configure OpenAPI / Swagger support (`MapOpenApi`, OpenAPI document endpoints).
- [x] Add consistent API response/error conventions (`ProblemDetails` RFC 7807, `PagedResult<T>`).
- [x] Add global exception handling (`ExceptionHandlingMiddleware`) and structured logging.
- [x] Add health endpoint (`/api/health`) verifying live database connectivity.
- [x] Confirm no secrets are committed (verified clean configuration with local connection strings).
**Gate:** API starts locally and basic endpoints work. (Gate Passed: verified via integration tests against live SQL Server).

## Phase 4 — Authentication and authorization

- [x] Choose a secure identity/password approach appropriate for the project (PBKDF2-HMAC-SHA256 with 100,000 iterations and 128-bit cryptographic salt).
- [x] Implement registration and login DTOs and validation with FluentValidation.
- [x] Store only secure password hashes; never plaintext passwords.
- [x] Configure JWT issuer, audience, signing key, and expiration with `Microsoft.AspNetCore.Authentication.JwtBearer`.
- [x] Implement role-based authorization policies (`RequireAdmin`, `RequireManager`, `RequireEmployee`).
- [x] Protect endpoints with server-side authorization and restrict public registration to `Employee` role.
- [x] Test invalid credentials, missing tokens (`401 Unauthorized`), and forbidden role access (`403 Forbidden`).
**Gate:** authenticated and unauthenticated access behave as expected. (Gate Passed: verified with 13 integration tests).

## Phase 5 — Departments and employee profiles

- [x] Implement department endpoints and business rules.
- [x] Implement employee profile endpoints.
- [x] Add pagination, search, filtering, and sorting where useful (`PagedResult<T>`).
- [x] Enforce role-based access and prevent unauthorized data exposure (Admin management, Manager paged search, self-view/update).
- [x] Add unit/integration tests (`DepartmentsEndpointTests`, `EmployeesEndpointTests`).
**Gate:** API behavior and authorization tests pass. (Gate Passed: verified with 55 total passing tests: 25 unit, 30 integration).

## Phase 6 — Leave management

- [x] Submit leave request (`POST /api/leaverequests`).
- [x] List own requests (`GET /api/leaverequests/my`); managers/admins list permitted requests (`GET /api/leaverequests`).
- [x] Approve/reject requests with reviewer, audit fields, and timestamp (`POST /api/leaverequests/{id}/approve`, `POST /api/leaverequests/{id}/reject`).
- [x] Prevent invalid dates and overlapping active requests (`HasActiveOverlappingRequestAsync`).
- [x] Define and enforce leave balance policy (`employee.DeductLeaveDays(daysCount)`).
- [x] Prevent self-approval and unauthorized review (`reviewer.Id != leaveRequest.EmployeeId`).
- [x] Test concurrency-sensitive and invalid transitions (`LeaveRequestsEndpointTests.cs`).
**Gate:** documented leave rules are enforced by tests. (Gate Passed: verified with 66 total passing tests: 25 unit, 41 integration).

## Phase 7 — Task management

- [x] Create and assign tasks (`POST /api/tasks`, `PUT /api/tasks/{id}`).
- [x] List tasks by assignee/status/priority/due date with pagination (`GET /api/tasks`).
- [x] Define permitted status transitions (`Todo` -> `InProgress` -> `InReview` -> `Done`, cancellation and changes-requested guards).
- [x] Enforce who can create, assign, edit, and complete tasks.
- [x] Add tests for invalid transitions and access control (`TasksEndpointTests.cs`).
**Gate:** task workflow tests pass. (Gate Passed: verified with 73 total passing tests: 25 unit, 48 integration).

## Phase 8 — Expense management

- [x] Submit expense with amount, category, expense date, currency, and description (`POST /api/expenses`).
- [x] Validate amount (>0) and required fields.
- [x] Review/approve/reject expenses with audit fields (`POST /api/expenses/{id}/approve`, `POST /api/expenses/{id}/reject`).
- [x] Prevent unauthorized review and self-approval (`reviewer.Id != expense.EmployeeId`).
- [x] Add pagination/filtering and tests (`ExpensesEndpointTests.cs`).
**Gate:** expense workflow tests pass. (Gate Passed: verified with 84 total passing tests: 25 unit, 59 integration).

## Phase 9 — Dashboard and quality

- [ ] Add dashboard summary endpoints based on database data.
- [ ] Optimize queries and use async EF Core calls.
- [ ] Add cancellation tokens where appropriate.
- [ ] Review validation, error responses, logging, and sensitive data handling.
- [ ] Add integration tests for key workflows.
- [ ] Add a useful test-data strategy for tests only; no fake data in production paths.
**Gate:** tests pass and dashboard counts are verified against known test data.

## Phase 10 — Documentation and release

- [ ] Complete README setup instructions.
- [ ] Document architecture, database diagram, endpoints, roles, and configuration.
- [ ] Add sanitized screenshots/API examples.
- [ ] Add `.gitignore` and ensure secrets/local files are excluded.
- [ ] Publish to GitHub.
- [ ] Choose an affordable and suitable deployment target.
- [ ] Deploy API and database only after reviewing cost, secrets, and security.
- [ ] Add portfolio project entry with problem, solution, features, stack, GitHub, and demo.
**Gate:** a new developer can follow the README to run the project; deployed behavior is verified if deployment is in scope.

## Phase 11 — Optional frontend

Only after the backend release gate passes:
- [ ] Choose React or Blazor based on portfolio goals.
- [ ] Build login, role-aware navigation, and dashboard.
- [ ] Build module screens and API error/loading states.
- [ ] Deploy frontend and connect it to the API.
