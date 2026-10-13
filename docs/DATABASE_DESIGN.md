# Database Design — Draft

Status: conceptual only. Finalize rules and relationships before generating migrations.

## Initial entities

### User
- `Id` (primary key)
- `Email` (required, unique, normalized for comparison)
- `PasswordHash` (required; never plaintext)
- `RoleId` or role/claims mapping (decision to finalize with identity design)
- `IsActive`
- `CreatedAtUtc`
- Optional lockout/security fields if needed by the selected identity approach

### Employee
- `Id`
- `UserId` (unique relationship to User, if every employee has a login)
- `FirstName`
- `LastName`
- `DepartmentId`
- Optional `ManagerEmployeeId` (self-reference, if the manager hierarchy is required)
- `HireDate`
- `IsActive`
- `CreatedAtUtc`

### Role
If a custom role table is used, define unique `Name` and relationships. If ASP.NET Core Identity is selected, use its role/user schema instead of duplicating it. Make this decision before migrations.

### Department
- `Id`
- `Name` (required, unique or unique within agreed scope)
- `Description` (optional)
- `CreatedAtUtc`

### LeaveRequest
- `Id`
- `EmployeeId`
- `StartDate` and `EndDate` (date-only concepts)
- `Reason`
- `Status` (Pending, Approved, Rejected, Cancelled — confirm allowed transitions)
- `ReviewedByEmployeeId` (nullable until reviewed)
- `ReviewedAtUtc` (nullable)
- `CreatedAtUtc`

### TaskItem
Use `TaskItem`, not `Task`, to avoid confusion with `System.Threading.Tasks.Task`.
- `Id`
- `Title`
- `Description`
- `AssignedToEmployeeId`
- `CreatedByEmployeeId`
- `Priority`
- `Status`
- `DueDate`
- `CreatedAtUtc`
- Optional `CompletedAtUtc`

### Expense
- `Id`
- `EmployeeId`
- `Category`
- `Amount` (decimal with explicit precision; never floating point for currency)
- `CurrencyCode` if multi-currency support is required; otherwise document the single-currency assumption
- `Description`
- `ExpenseDate`
- `Status` (Pending, Approved, Rejected, Cancelled — confirm transitions)
- `ReviewedByEmployeeId`
- `ReviewedAtUtc`
- `CreatedAtUtc`

## Relationship sketch

```text
Department 1 ─── * Employee
User       1 ─── 0..1 Employee
Employee   1 ─── * LeaveRequest
Employee   1 ─── * TaskItem (assignee)
Employee   1 ─── * TaskItem (creator)
Employee   1 ─── * Expense
Employee   1 ─── * LeaveRequest (reviewer, optional)
Employee   1 ─── * Expense (reviewer, optional)
```

A user-to-employee one-to-one relation is a proposed default, not a confirmed requirement. Finalize whether non-employee Admin accounts exist and how manager accounts relate to employee profiles.

## Rules to decide before migrations

- Identity approach: ASP.NET Core Identity versus a custom user table. Prefer Identity unless a specific learning constraint justifies custom authentication.
- Role assignment and initial admin provisioning.
- Leave calendar: weekends/holidays, inclusive date count, annual balance, and cancellation behavior.
- Overlap definition and whether rejected/cancelled requests are excluded.
- Manager scope: department manager, assigned manager, or policy-based access.
- Whether managers may approve their own requests (default: no).
- Task status transition rules and who may reassign tasks.
- Expense amount limits, currency, and receipt uploads (receipt uploads are not initial scope unless added deliberately).
- Delete behavior: prefer soft deactivation or restrictive deletes for audit-sensitive records.
- Indexes for email, foreign keys, status/date filters, and frequently used queries.

## Data safety

- Never use real personal data in test fixtures.
- Development seed data, if needed, must be explicitly development-only and use fake data.
- Production database paths must use real persisted records, not hardcoded demonstration responses.
