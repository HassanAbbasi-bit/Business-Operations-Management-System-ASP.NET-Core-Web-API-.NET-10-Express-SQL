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

As of 2026-10-13, Phase 0 is complete:
- Solution scaffold: **Confirmed created** (`BusinessOperations.slnx`)
- Clean Architecture dependencies: **Confirmed wired**
- Restore & Build: **Confirmed passing (0 Warnings, 0 Errors)**
- Baseline tests: **Confirmed passing (UnitTests & IntegrationTests)**
- Database engine: **Confirmed running on `.\SQLEXPRESS`**
- Git repository: **Confirmed initialized**
- Entities/migrations/endpoints: **Next phase (Phase 1 Domain Modeling)**

Never describe unverified work as completed. Update this file when verified facts change.
