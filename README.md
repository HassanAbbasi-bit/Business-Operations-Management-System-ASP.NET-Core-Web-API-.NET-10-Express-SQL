# Business Operations Management System

A portfolio-grade business application built with ASP.NET Core Web API and SQL Server. The first delivery focuses on a reliable backend; a React or Blazor frontend may be added after the API is stable.

## Project goals

- Demonstrate practical .NET backend engineering, not just basic CRUD.
- Implement authentication, authorization, validation, business workflows, persistence, logging, and automated tests.
- Keep the architecture understandable and avoid unnecessary abstractions.
- Maintain a verifiable record of decisions, completed work, known issues, and the next task.

## Technology baseline

- .NET SDK: `10.0.401` confirmed on the developer machine.
- ASP.NET Core Web API / C#
- SQL Server
- Entity Framework Core
- JWT bearer authentication and role-based authorization
- FluentValidation
- OpenAPI / Swagger
- Serilog
- xUnit
- Git and GitHub

Exact package versions should be selected for compatibility with .NET 10 at implementation time and recorded in the project files. Do not blindly copy package versions from old tutorials.

## Solution layout

```text
BusinessOperations/
├── BusinessOperations.slnx
├── README.md
├── docs/
│   ├── PROJECT_CONTEXT.md
│   ├── PROJECT_PLAN.md
│   ├── ARCHITECTURE.md
│   ├── TECH_STACK.md
│   ├── DATABASE_DESIGN.md
│   ├── API_CONTRACT.md
│   ├── TASK_CHECKLIST.md
│   ├── DEVELOPMENT_LOG.md
│   └── AI_WORKING_RULES.md
├── src/
│   ├── BusinessOperations.API/
│   ├── BusinessOperations.Application/
│   ├── BusinessOperations.Domain/
│   └── BusinessOperations.Infrastructure/
└── tests/
    ├── BusinessOperations.UnitTests/
    └── BusinessOperations.IntegrationTests/
```

The `docs/` directory is at the repository root. The implementation scaffold is configured, built, and verified.

## Planned business modules

1. Authentication and users
2. Departments and employee profiles
3. Leave requests and approval workflow
4. Task assignment and status workflow
5. Expense submissions and approval workflow
6. Dashboard summaries
7. Cross-cutting error handling, validation, logging, and testing

## How to work on this project

1. Read `docs/PROJECT_CONTEXT.md`.
2. Read `docs/TASK_CHECKLIST.md` and `docs/DEVELOPMENT_LOG.md`.
3. Work on the first unchecked task in `docs/PROJECT_PLAN.md`, unless the user explicitly changes priorities.
4. Explain major work using **What, Why, How, Code, Test**.
5. Run relevant checks and report actual outcomes. Never mark a task complete just because code was suggested.
6. Update the checklist and development log after each meaningful milestone.

## Current status

Phase 0 (Environment and scaffold) is complete and verified:
- .NET SDK `10.0.401` confirmed.
- SQL Server Express (`.\SQLEXPRESS`) confirmed running (Microsoft SQL Server 2025 Express).
- Solution `BusinessOperations.slnx` with all 6 projects (API, Application, Domain, Infrastructure, UnitTests, IntegrationTests) builds with 0 errors and passes baseline tests.
- Proceeding to Phase 1: Requirements and domain model.
