# Project Context — Source of Truth

Last updated: 2026-10-13

## Purpose

Build a real-life, portfolio-grade Business Operations Management System to help the developer learn and demonstrate professional ASP.NET Core backend development. The API comes first; a frontend is deferred until the backend is stable.

## Developer environment confirmed

- Windows desktop environment.
- `dotnet --version` returned `10.0.401`.
- `dotnet --list-sdks` showed `10.0.401` at `C:\Program Files\dotnet\sdk`.
- `sqlcmd -?` returned SQL Server Command Line Tool version `15.0.1300.359`.
- SQL Server service `MSSQL$SQLEXPRESS` is running and verified accessible locally via `.\SQLEXPRESS` (Microsoft SQL Server 2025 Express, 17.0.1135.8).

## Agreed project and names

- Solution: `BusinessOperations.slnx`
- API: `BusinessOperations.API`
- Application: `BusinessOperations.Application`
- Domain: `BusinessOperations.Domain`
- Infrastructure: `BusinessOperations.Infrastructure`
- Unit tests: `BusinessOperations.UnitTests`
- Integration tests: `BusinessOperations.IntegrationTests`

Target framework: `net10.0`.

## Agreed architecture direction

A pragmatic Clean Architecture-style solution:

- API depends on Application and Infrastructure.
- Application depends on Domain.
- Infrastructure depends on Application and Domain.
- Domain has no project dependency on the other layers.
- Unit tests reference Application and Domain.
- Integration tests reference API; add other references only when genuinely needed.

Keep this architecture lightweight. Do not introduce MediatR, CQRS, a generic repository framework, Docker, Redis, microservices, or other infrastructure by default. Add a dependency only when there is a clear requirement and explain the trade-off first.

## Agreed learning style

For each major task, explain:
1. **What** we are building.
2. **Why** it is needed.
3. **How** it fits the architecture.
4. **Code** with file paths and complete enough implementation details.
5. **Test** with commands or API requests and expected results.

The developer wants to understand the code rather than blindly copy it. Give step-by-step instructions and avoid skipping prerequisites.

## Planned business scope

- Authentication: register/login, secure password hashing, JWT validation, roles.
- Roles: Admin, Manager, Employee.
- Departments and employee profiles.
- Leave requests: submit, review, approve/reject, leave-balance rules.
- Tasks: create/assign, priority, due date, status transitions.
- Expenses: submit, review, approve/reject, amount/category/date.
- Dashboard aggregates.
- Pagination, search, filtering where appropriate.
- Centralized exception handling, validation responses, structured logging.
- Unit and integration tests.
- OpenAPI documentation and deployment/readme material.

Scope is planned, not implemented. Refine requirements before coding each module.

## Important status rule

As of 2026-10-13:
- Solution scaffold (Phase 0): **Confirmed created & passing** (`BusinessOperations.slnx`)
- Domain model (Phase 1): **Confirmed implemented & verified** (25 unit tests passing)
- Persistence foundation (Phase 2): **Confirmed configured & migrated** (EF Core 10.0.12, `ApplicationDbContext`, `InitialCreate` migration applied to `.\SQLEXPRESS` creating `BusinessOperationsDb` with all 7 tables)
- API foundation (Phase 3): **Confirmed implemented & verified** (`ExceptionHandlingMiddleware` RFC 7807 ProblemDetails, `HealthController` with live DB check)
- Authentication & Authorization (Phase 4): **Confirmed implemented & verified** (PBKDF2 password hashing, JWT Bearer tokens, registration, login, role policies `RequireAdmin`/`RequireManager`/`RequireEmployee`)
- Departments & Employee Profiles (Phase 5): **Confirmed implemented & verified** (`DepartmentsController`, `EmployeesController`, pagination/search, role barriers, tamper protection)
- Leave Management (Phase 6): **Confirmed implemented & verified** (`LeaveRequestsController`, submission, overlap check, balance deduction, approval/rejection lifecycle, self-approval prevention)
- Task Management (Phase 7): **Confirmed implemented & verified** (`TasksController`, assignment, state machine transitions, cancellation guards)
- Expense Management (Phase 8): **Confirmed implemented & verified** (`ExpensesController`, amount/category validations, review workflow, self-approval prevention)
- Dashboard and Quality (Phase 9): **Confirmed implemented & verified** (`DashboardController`, executive summary & personal operational metrics, cancellation tokens, async aggregations)
- Documentation and Release (Phase 10): **Confirmed completed & verified** (Comprehensive portfolio README, API contract, and release guides)
- Modern React Dashboard (Phase 11): **Confirmed implemented & verified** (Vite + React + Vanilla CSS dark mode command center, quick demo role switcher, executive metrics, task matrix, leave approvals, and expense claims)
- Tests: **Confirmed passing** (88 tests total: 25 unit tests, 63 integration tests; 0 warnings, 0 errors)
- Current Milestone: **Full Stack Application Complete (Phases 0–11). Portfolio-ready backend and frontend.**

Never describe unverified work as completed. Update this file when verified facts change.
