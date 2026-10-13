# API Contract — Planned

This is a proposed endpoint map, not a claim that endpoints exist. Confirm route names, response DTOs, pagination format, and permissions during implementation.

## Conventions

- Prefix: `/api`
- JSON request/response bodies.
- Use appropriate HTTP status codes.
- Use DTOs, not database entities, in public responses.
- Use consistent validation and error response shapes.
- Paginate collection endpoints.
- Use UTC timestamps for event timestamps; use date-only formats for leave/expense dates.
- Require bearer authentication except for explicitly public endpoints such as registration/login.

## Authentication

| Method | Route | Purpose | Access |
|---|---|---|---|
| POST | `/api/auth/register` | Register an eligible user | Public or restricted by registration policy |
| POST | `/api/auth/login` | Authenticate and issue token | Public |

Registration must not allow arbitrary callers to grant themselves Admin or Manager roles.

## Departments and employees

| Method | Route | Purpose | Planned access |
|---|---|---|---|
| GET | `/api/departments` | List departments | Authenticated; refine by role |
| GET | `/api/departments/{id}` | Department details | Authorized |
| POST | `/api/departments` | Create department | Admin |
| PUT | `/api/departments/{id}` | Update department | Admin |
| GET | `/api/employees` | Search/list employees | Admin/Manager; scope results |
| GET | `/api/employees/{id}` | Employee details | Self or authorized manager/admin |
| POST | `/api/employees` | Create employee profile | Admin |
| PUT | `/api/employees/{id}` | Update profile | Self for allowed fields; Admin for privileged fields |

## Leave requests

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/leaves` | Submit leave request |
| GET | `/api/leaves/my` | List caller's requests |
| GET | `/api/leaves` | List permitted requests with filters |
| GET | `/api/leaves/{id}` | View an authorized request |
| POST | `/api/leaves/{id}/approve` | Approve a pending request |
| POST | `/api/leaves/{id}/reject` | Reject a pending request |
| POST | `/api/leaves/{id}/cancel` | Cancel if business rules allow |

## Tasks

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/tasks` | Create task |
| GET | `/api/tasks` | List permitted tasks with filters |
| GET | `/api/tasks/{id}` | Task details |
| PUT | `/api/tasks/{id}` | Update permitted task fields |
| PATCH | `/api/tasks/{id}/status` | Change status using validated transitions |

## Expenses

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/expenses` | Submit expense |
| GET | `/api/expenses/my` | List caller's expenses |
| GET | `/api/expenses` | List permitted expenses with filters |
| GET | `/api/expenses/{id}` | View an authorized expense |
| POST | `/api/expenses/{id}/approve` | Approve pending expense |
| POST | `/api/expenses/{id}/reject` | Reject pending expense |

## Dashboard

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/dashboard/summary` | Return counts scoped to the caller's permissions |

Do not expose global company statistics to an Employee unless that is an explicit policy decision.

## Standard status expectations

- `200 OK`: successful read/update where a response is returned.
- `201 Created`: resource created; include a location when practical.
- `204 No Content`: successful operation without a body.
- `400 Bad Request`: malformed/invalid request or validation failure (choose one consistent validation convention).
- `401 Unauthorized`: missing/invalid authentication.
- `403 Forbidden`: authenticated but not allowed.
- `404 Not Found`: resource does not exist or is intentionally concealed by authorization policy.
- `409 Conflict`: conflicting state, duplicate, or invalid workflow transition where appropriate.

Use `ProblemDetails`/`ValidationProblemDetails` or another consistent documented shape.
