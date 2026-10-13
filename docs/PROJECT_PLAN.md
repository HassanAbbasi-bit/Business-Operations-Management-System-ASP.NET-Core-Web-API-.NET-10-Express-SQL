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

- [ ] Select compatible EF Core 10 packages.
- [ ] Configure SQL Server connection string through local configuration/user secrets.
- [ ] Add `ApplicationDbContext` and entity configurations.
- [ ] Configure indexes, constraints, relationships, and delete behavior.
- [ ] Create first migration and database.
- [ ] Verify schema and migration reproducibility.
**Gate:** app connects to the intended local SQL Server instance and migrations work.

## Phase 3 — API foundation

- [ ] Configure dependency injection and application services.
- [ ] Configure OpenAPI/Swagger.
- [ ] Add consistent API response/error conventions.
- [ ] Add global exception handling and structured logging.
- [ ] Add health endpoint if useful.
- [ ] Confirm no secrets are committed.
**Gate:** API starts locally and basic endpoints work.

## Phase 4 — Authentication and authorization

- [ ] Choose a secure identity/password approach appropriate for the project.
- [ ] Implement registration and login DTOs/validation.
- [ ] Store only secure password hashes; never plaintext passwords.
- [ ] Configure JWT issuer, audience, signing key, and expiration.
- [ ] Implement role-based authorization policies.
- [ ] Seed initial roles safely; never seed a known production password.
- [ ] Test invalid credentials, token expiry/configuration, and forbidden role access.
**Gate:** authenticated and unauthenticated access behave as expected.

## Phase 5 — Departments and employee profiles

- [ ] Implement department endpoints and business rules.
- [ ] Implement employee profile endpoints.
- [ ] Add pagination, search, filtering, and sorting where useful.
- [ ] Enforce role-based access and prevent unauthorized data exposure.
- [ ] Add unit/integration tests.
**Gate:** API behavior and authorization tests pass.

## Phase 6 — Leave management

- [ ] Submit leave request.
- [ ] List own requests; managers/admins list permitted requests.
- [ ] Approve/reject requests with reviewer and timestamp.
- [ ] Prevent invalid dates and overlapping active requests.
- [ ] Define and enforce leave balance policy.
- [ ] Prevent self-approval and unauthorized review.
- [ ] Test concurrency-sensitive and invalid transitions.
**Gate:** documented leave rules are enforced by tests.

## Phase 7 — Task management

- [ ] Create and assign tasks.
- [ ] List tasks by assignee/status/priority/due date.
- [ ] Define permitted status transitions.
- [ ] Enforce who can create, assign, edit, and complete tasks.
- [ ] Add tests for invalid transitions and access control.
**Gate:** task workflow tests pass.

## Phase 8 — Expense management

- [ ] Submit expense with amount, category, expense date, and description.
- [ ] Validate amount and required fields.
- [ ] Review/approve/reject expenses with audit fields.
- [ ] Prevent unauthorized review and self-approval.
- [ ] Add pagination/filtering and tests.
**Gate:** expense workflow tests pass.

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
