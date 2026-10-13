# API Contract — Implemented

All endpoints are implemented in `BusinessOperations.API` and verified by automated integration tests.

## Conventions

- Prefix: `/api`
- JSON request/response bodies.
- Standard HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 NotFound`).
- RFC 7807 `ProblemDetails` for errors.
- Strong typed DTOs in responses.
- Paginated responses wrapped in `PagedResult<T>` with `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage`.
- JWT Bearer authentication on protected endpoints.

## Authentication (`/api/auth`)

| Method | Route | Description | Access |
|---|---|---|---|
| POST | `/api/auth/register` | Register new user account with `Employee` role | Public |
| POST | `/api/auth/login` | Authenticate and issue signed JWT bearer token | Public |
| GET | `/api/auth/me` | Retrieve profile and role claims of authenticated caller | `[Authorize]` |
| GET | `/api/auth/admin-only` | Administrative access policy check | `RequireAdmin` |
| GET | `/api/auth/manager-only`| Management access policy check | `RequireManager` |

## Departments (`/api/departments`)

| Method | Route | Description | Access |
|---|---|---|---|
| GET | `/api/departments` | List all departments with employee counts | `[Authorize]` |
| GET | `/api/departments/{id}` | Retrieve department by ID | `[Authorize]` |
| POST | `/api/departments` | Create new department | `RequireAdmin` |
| PUT | `/api/departments/{id}` | Update department name and manager | `RequireAdmin` |

## Employees (`/api/employees`)

| Method | Route | Description | Access |
|---|---|---|---|
| GET | `/api/employees` | Search and filter employees (paged) | `RequireManager` |
| GET | `/api/employees/{id}` | Retrieve employee profile | Submitter, Manager, or Admin |
| POST | `/api/employees` | Create employee profile | `RequireAdmin` |
| PUT | `/api/employees/{id}` | Update employee profile (tamper-protected) | Self (names/email) or Admin (all) |

## Leave Requests (`/api/leaverequests`)

| Method | Route | Description | Access |
|---|---|---|---|
| POST | `/api/leaverequests` | Submit leave request with date and balance validation | `[Authorize]` |
| GET | `/api/leaverequests/my` | Retrieve authenticated caller's leave history | `[Authorize]` |
| GET | `/api/leaverequests` | Query leave requests with status/department filters (paged) | `RequireManager` |
| GET | `/api/leaverequests/{id}` | Retrieve single leave request | Submitter, Manager, or Admin |
| POST | `/api/leaverequests/{id}/approve` | Approve leave and deduct balance (prevents self-approval) | `RequireManager` |
| POST | `/api/leaverequests/{id}/reject` | Reject leave request with required reason | `RequireManager` |
| POST | `/api/leaverequests/{id}/cancel` | Cancel pending leave request | Submitter |

## Tasks (`/api/tasks`)

| Method | Route | Description | Access |
|---|---|---|---|
| GET | `/api/tasks` | List tasks with status/priority/assignee filters (paged) | `[Authorize]` |
| GET | `/api/tasks/{id}` | Retrieve task details | `[Authorize]` |
| POST | `/api/tasks` | Create task assigned to employee | `[Authorize]` |
| PUT | `/api/tasks/{id}` | Update task details, assignee, priority | `[Authorize]` |
| POST | `/api/tasks/{id}/start` | Transition status: `Todo` -> `InProgress` | `[Authorize]` |
| POST | `/api/tasks/{id}/review` | Transition status: `InProgress` -> `InReview` | `[Authorize]` |
| POST | `/api/tasks/{id}/request-changes` | Transition status: `InReview` -> `InProgress` | `[Authorize]` |
| POST | `/api/tasks/{id}/complete` | Mark task complete: `Done` | `[Authorize]` |
| POST | `/api/tasks/{id}/cancel` | Cancel task (from non-done status) | `[Authorize]` |

## Expenses (`/api/expenses`)

| Method | Route | Description | Access |
|---|---|---|---|
| POST | `/api/expenses` | Submit expense reimbursement request | `[Authorize]` |
| GET | `/api/expenses/my` | Retrieve caller's submitted expenses | `[Authorize]` |
| GET | `/api/expenses` | Query expenses with category/status filters (paged) | `RequireManager` |
| GET | `/api/expenses/{id}` | Retrieve single expense reimbursement | Submitter, Manager, or Admin |
| POST | `/api/expenses/{id}/approve` | Approve expense reimbursement (prevents self-approval) | `RequireManager` |
| POST | `/api/expenses/{id}/reject` | Reject expense with required audit reason | `RequireManager` |
| POST | `/api/expenses/{id}/cancel` | Cancel pending expense submission | Submitter |

## Dashboard (`/api/dashboard`)

| Method | Route | Description | Access |
|---|---|---|---|
| GET | `/api/dashboard/summary` | Organization-wide executive metrics aggregate | `RequireManager` |
| GET | `/api/dashboard/my` | Personal operational metrics for authenticated employee | `[Authorize]` |

## System Health (`/api/health`)

| Method | Route | Description | Access |
|---|---|---|---|
| GET | `/api/health` | Live database connectivity and health probe | Public |
