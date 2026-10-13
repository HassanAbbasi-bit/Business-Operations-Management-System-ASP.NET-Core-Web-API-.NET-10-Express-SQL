# Business Operations Management System

A portfolio-grade enterprise backend built with **ASP.NET Core Web API (.NET 10)**, **Clean Architecture**, and **SQL Server**. It models core business operations including identity, department hierarchies, employee profiles, leave request workflows, task lifecycle tracking, expense reimbursements, and executive dashboard metrics.

---

## Architecture Overview

This solution strictly implements **Clean Architecture** principles to separate concerns, enforce business invariants within the domain model, and decouple infrastructure from core application logic.

```text
BusinessOperations/
├── BusinessOperations.slnx
├── src/
│   ├── BusinessOperations.Domain/         # Pure domain entities, value objects, enums, domain exceptions
│   ├── BusinessOperations.Application/    # DTOs, interfaces, services, validators (FluentValidation)
│   ├── BusinessOperations.Infrastructure/ # EF Core 10, SQL Server, repositories, password hashing, JWT
│   └── BusinessOperations.API/            # Controllers, middleware, OpenAPI, authorization policies
└── tests/
    ├── BusinessOperations.UnitTests/        # Domain invariant and rule unit tests
    └── BusinessOperations.IntegrationTests/ # Live endpoint integration tests with WebApplicationFactory
```

### Dependency Direction
- **API** depends on **Application** and **Infrastructure**.
- **Infrastructure** depends on **Application** and **Domain**.
- **Application** depends on **Domain**.
- **Domain** has no external dependencies.
- **UnitTests** depends on **Application** and **Domain**.
- **IntegrationTests** depends on **API**.

---

## Key Features

1. **Authentication & Role-Based Authorization**:
   - Secure PBKDF2-HMAC-SHA256 password hashing (100,000 iterations, 128-bit cryptographic salt, constant-time verification).
   - Signed JWT Bearer tokens with custom identity claims (`sub`, `email`, `role`, `employeeId`, `jti`).
   - Granular role policies (`RequireAdmin`, `RequireManager`, `RequireEmployee`).
2. **Departments & Employee Management**:
   - Organizational department hierarchy with manager assignments.
   - Employee directory with search, filtering, and pagination (`PagedResult<T>`).
   - Profile update security with tamper protection (non-admins cannot alter their department or leave balance).
3. **Leave Request Workflow**:
   - Calendar day calculation with inclusive duration.
   - Prevention of overlapping active requests.
   - Leave balance verification and automated deduction upon manager approval.
   - Strict self-approval prevention (managers cannot approve their own leaves).
   - Cancellation permitted only on pending requests by the submitting employee.
4. **Task Lifecycle Management**:
   - Explicit state machine: `Todo` -> `InProgress` -> `InReview` -> `Done`.
   - Re-work lifecycle: `InReview` -> `InProgress` (Request Changes).
   - Cancellation guards (cannot cancel completed tasks).
5. **Expense Reimbursement Workflow**:
   - Multi-category expenditures (`Travel`, `Meals`, `Equipment`, `OfficeSupplies`, `Training`, `Other`).
   - Non-future date validation and monetary rounding with 2 decimal precision.
   - Approval audit trail with reviewer tracking and required rejection reasons.
   - Strict self-approval prevention.
6. **Executive & Personal Dashboards**:
   - Executive metrics for managers/admins (active employees, departments, task status breakdown, pending/approved leave counts, total pending/approved expense amounts).
   - Personal metrics for individual employees (assigned tasks, pending requests, remaining leave balance).
7. **Production Quality & Resilience**:
   - Global `ProblemDetails` exception handling middleware (RFC 7807).
   - Structured logging via ASP.NET Core abstractions.
   - Cancellation token propagation across all database queries.
   - Live health check endpoint (`/api/health`) verifying SQL Server connectivity.

---

## Technology Stack

- **Framework**: .NET 10 (`net10.0`, C# 13)
- **Database**: Microsoft SQL Server / SQL Server Express (`.\SQLEXPRESS`)
- **ORM**: Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer` 10.0.12)
- **Validation**: FluentValidation 12.2.0
- **Authentication**: `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12
- **Documentation**: OpenAPI / Swagger (`Microsoft.AspNetCore.OpenApi`)
- **Testing**: xUnit, `Microsoft.AspNetCore.Mvc.Testing` 10.0.12

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/) (`10.0.401` or later).
- [SQL Server Express](https://www.microsoft.com/sql-server) running locally as `.\SQLEXPRESS`.

### Configuration
Database connection and JWT settings are configured in `src/BusinessOperations.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=BusinessOperationsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "BusinessOperationsDefaultDevelopmentSecretKey_AtLeast32CharsLong!",
    "Issuer": "BusinessOperationsApi",
    "Audience": "BusinessOperationsClient",
    "ExpirationMinutes": 120
  }
}
```

### Database Migration
Apply the migrations using EF Core tools:
```powershell
dotnet tool restore
dotnet ef database update --project src/BusinessOperations.Infrastructure --startup-project src/BusinessOperations.API
```

### Running the API
```powershell
dotnet run --project src/BusinessOperations.API
```
Once launched, inspect OpenAPI documentation at `https://localhost:5001/openapi/v1.json` or Swagger UI.

---

## Automated Test Suite

The solution contains a verified test suite covering domain business invariants, validation rules, security boundaries, and live API endpoints:

```powershell
dotnet test
```

### Test Coverage Highlights:
- **Unit Tests (`BusinessOperations.UnitTests`)**: 25 tests validating domain invariants, leave day calculations, invalid state transitions, and expense rules.
- **Integration Tests (`BusinessOperations.IntegrationTests`)**: 63 live integration tests verifying:
  - Auth registration, credential verification, duplicate prevention, and JWT token issuance.
  - Role-based 401 Unauthorized and 403 Forbidden boundaries.
  - Department and Employee CRUD with pagination and search.
  - Leave submission, balance deduction, overlap prevention, and self-approval blocking.
  - Task state machine transitions (`start`, `review`, `request-changes`, `complete`, `cancel`).
  - Expense submissions, category audits, and required rejection notes.
  - Organizational and personal dashboard metrics against live database state.

---

## API Endpoints Summary

| Module | Endpoint | Method | Access | Description |
|---|---|---|---|---|
| **Health** | `/api/health` | `GET` | Public | Live database connectivity probe |
| **Auth** | `/api/auth/register` | `POST` | Public | Register new user account (`Employee` role) |
| **Auth** | `/api/auth/login` | `POST` | Public | Issue signed JWT bearer token |
| **Auth** | `/api/auth/me` | `GET` | Authenticated | Current user profile and roles |
| **Departments** | `/api/departments` | `GET` | Authenticated | List all departments |
| **Departments** | `/api/departments/{id}` | `GET` | Authenticated | View department by ID |
| **Departments** | `/api/departments` | `POST` | Admin | Create department |
| **Departments** | `/api/departments/{id}` | `PUT` | Admin | Update department |
| **Employees** | `/api/employees` | `GET` | Manager/Admin | Search/filter employees (paged) |
| **Employees** | `/api/employees/{id}` | `GET` | Self/Manager/Admin | View employee profile |
| **Employees** | `/api/employees` | `POST` | Admin | Create employee profile |
| **Employees** | `/api/employees/{id}` | `PUT` | Self/Admin | Update profile (tamper-protected) |
| **Leave** | `/api/leaverequests` | `POST` | Authenticated | Submit leave request |
| **Leave** | `/api/leaverequests/my` | `GET` | Authenticated | View caller's leave requests |
| **Leave** | `/api/leaverequests` | `GET` | Manager/Admin | Filter leave requests (paged) |
| **Leave** | `/api/leaverequests/{id}/approve` | `POST` | Manager/Admin | Approve leave (self-approval blocked) |
| **Leave** | `/api/leaverequests/{id}/reject` | `POST` | Manager/Admin | Reject leave with required reason |
| **Leave** | `/api/leaverequests/{id}/cancel` | `POST` | Authenticated | Cancel pending leave request |
| **Tasks** | `/api/tasks` | `GET` | Authenticated | List tasks with filters (paged) |
| **Tasks** | `/api/tasks/{id}` | `GET` | Authenticated | View task by ID |
| **Tasks** | `/api/tasks` | `POST` | Authenticated | Create task |
| **Tasks** | `/api/tasks/{id}` | `PUT` | Authenticated | Update task details |
| **Tasks** | `/api/tasks/{id}/start` | `POST` | Authenticated | Start task (`Todo` -> `InProgress`) |
| **Tasks** | `/api/tasks/{id}/review` | `POST` | Authenticated | Submit for review (`InProgress` -> `InReview`) |
| **Tasks** | `/api/tasks/{id}/request-changes` | `POST` | Authenticated | Request changes (`InReview` -> `InProgress`) |
| **Tasks** | `/api/tasks/{id}/complete` | `POST` | Authenticated | Mark task `Done` |
| **Tasks** | `/api/tasks/{id}/cancel` | `POST` | Authenticated | Cancel task |
| **Expenses** | `/api/expenses` | `POST` | Authenticated | Submit expense reimbursement |
| **Expenses** | `/api/expenses/my` | `GET` | Authenticated | View caller's expenses |
| **Expenses** | `/api/expenses` | `GET` | Manager/Admin | Filter expenses (paged) |
| **Expenses** | `/api/expenses/{id}/approve` | `POST` | Manager/Admin | Approve reimbursement |
| **Expenses** | `/api/expenses/{id}/reject` | `POST` | Manager/Admin | Reject reimbursement with reason |
| **Expenses** | `/api/expenses/{id}/cancel` | `POST` | Authenticated | Cancel pending reimbursement |
| **Dashboard** | `/api/dashboard/summary` | `GET` | Manager/Admin | Organization aggregate metrics |
| **Dashboard** | `/api/dashboard/my` | `GET` | Authenticated | Personal employee metrics |

---

## License
MIT
