# Database Design — Finalized Specification

Status: Finalized for Phase 1. Ready for EF Core 10 entity configuration and migrations.

## Entities and Schema Details

### User
Represents system accounts for authentication and JWT claims issuance.
- `Id` (PK, uniqueidentifier, Guid)
- `Email` (nvarchar(256), required, unique index, normalized lowercase)
- `PasswordHash` (nvarchar(max), required; salted and hashed)
- `Role` (int/nvarchar(20), required; mapped to `UserRole` enum: 1=Employee, 2=Manager, 3=Admin)
- `IsActive` (bit, required, default 1)
- `CreatedAtUtc` (datetime2(7), required)
- `UpdatedAtUtc` (datetime2(7), nullable)

### Department
Organizational units within the company.
- `Id` (PK, uniqueidentifier, Guid)
- `Name` (nvarchar(100), required, unique index)
- `Description` (nvarchar(500), nullable)
- `ManagerEmployeeId` (uniqueidentifier, nullable; FK to `Employee`)
- `CreatedAtUtc` (datetime2(7), required)
- `UpdatedAtUtc` (datetime2(7), nullable)

### Employee
Individual employee profile holding organizational hierarchy and leave entitlements.
- `Id` (PK, uniqueidentifier, Guid)
- `UserId` (uniqueidentifier, nullable, unique index; 1-to-0..1 relationship with `User`)
- `FirstName` (nvarchar(50), required)
- `LastName` (nvarchar(50), required)
- `Email` (nvarchar(256), required, unique index)
- `DepartmentId` (uniqueidentifier, required; FK to `Department`, DeleteBehavior.Restrict)
- `ManagerId` (uniqueidentifier, nullable; self-referencing FK to `Employee`, DeleteBehavior.Restrict)
- `HireDate` (date, required)
- `AnnualLeaveBalanceDays` (int, required, default 20)
- `IsActive` (bit, required, default 1)
- `CreatedAtUtc` (datetime2(7), required)
- `UpdatedAtUtc` (datetime2(7), nullable)

### LeaveRequest
Employee absence requests and management review lifecycle.
- `Id` (PK, uniqueidentifier, Guid)
- `EmployeeId` (uniqueidentifier, required; FK to `Employee`, DeleteBehavior.Restrict)
- `StartDate` (date, required)
- `EndDate` (date, required)
- `Reason` (nvarchar(500), required)
- `Status` (int, required; mapped to `LeaveStatus` enum: 1=Pending, 2=Approved, 3=Rejected, 4=Cancelled)
- `ReviewedByEmployeeId` (uniqueidentifier, nullable; FK to `Employee`, DeleteBehavior.Restrict)
- `ReviewedAtUtc` (datetime2(7), nullable)
- `ReviewNotes` (nvarchar(500), nullable)
- `CreatedAtUtc` (datetime2(7), required)
- `UpdatedAtUtc` (datetime2(7), nullable)

### TaskItem
Operational tasks with assignment, priority, and lifecycle state.
- `Id` (PK, uniqueidentifier, Guid)
- `Title` (nvarchar(200), required)
- `Description` (nvarchar(2000), nullable)
- `AssignedToEmployeeId` (uniqueidentifier, nullable; FK to `Employee`, DeleteBehavior.SetNull)
- `CreatedByEmployeeId` (uniqueidentifier, required; FK to `Employee`, DeleteBehavior.Restrict)
- `Priority` (int, required; mapped to `TaskPriority` enum: 1=Low, 2=Medium, 3=High, 4=Urgent)
- `Status` (int, required; mapped to `TaskItemStatus` enum: 1=Todo, 2=InProgress, 3=InReview, 4=Done, 5=Cancelled)
- `DueDateUtc` (datetime2(7), nullable)
- `CompletedAtUtc` (datetime2(7), nullable)
- `CreatedAtUtc` (datetime2(7), required)
- `UpdatedAtUtc` (datetime2(7), nullable)

### Expense
Business expense reimbursement requests and approval lifecycle.
- `Id` (PK, uniqueidentifier, Guid)
- `EmployeeId` (uniqueidentifier, required; FK to `Employee`, DeleteBehavior.Restrict)
- `Category` (int, required; mapped to `ExpenseCategory` enum: 1=Travel, 2=Meals, 3=Equipment, 4=OfficeSupplies, 5=Training, 6=Other)
- `Amount` (decimal(18,2), required; check constraint > 0)
- `Currency` (char(3), required, default 'USD')
- `Description` (nvarchar(500), required)
- `ExpenseDate` (date, required)
- `Status` (int, required; mapped to `ExpenseStatus` enum: 1=Pending, 2=Approved, 3=Rejected, 4=Cancelled)
- `ReviewedByEmployeeId` (uniqueidentifier, nullable; FK to `Employee`, DeleteBehavior.Restrict)
- `ReviewedAtUtc` (datetime2(7), nullable)
- `ReviewNotes` (nvarchar(500), nullable)
- `CreatedAtUtc` (datetime2(7), required)
- `UpdatedAtUtc` (datetime2(7), nullable)

## Relationships and Foreign Keys

```text
Department   1 ───────< * Employee (DepartmentId)
Employee     1 ───────< 0..1 Department (Department.ManagerEmployeeId)
User         1 ───────< 0..1 Employee (UserId, unique)
Employee     1 (Manager) ───< * Employee (DirectReports via ManagerId)
Employee     1 ───────< * LeaveRequest (EmployeeId)
Employee     1 ───────< * LeaveRequest (ReviewedByEmployeeId, optional)
Employee     1 ───────< * TaskItem (CreatedTasks via CreatedByEmployeeId)
Employee     1 ───────< * TaskItem (AssignedTasks via AssignedToEmployeeId, optional)
Employee     1 ───────< * Expense (EmployeeId)
Employee     1 ───────< * Expense (ReviewedByEmployeeId, optional)
```

## Business Rules and Enforced Invariants

1. **Authentication and Identity**:
   - Clean Architecture independence: A dedicated `User` entity keeps the domain pure and decoupled from ASP.NET Identity infrastructure.
   - Passwords must be hashed using a standard secure algorithm (e.g., ASP.NET Core PasswordHasher / PBKDF2 / BCrypt). Plaintext passwords are strictly forbidden.
   - User emails are normalized to lowercase and enforced with unique indexes.

2. **Self-Review Prevention**:
   - Neither `LeaveRequest` nor `Expense` may be approved or reviewed by the submitting employee (`ReviewedByEmployeeId != EmployeeId`).

3. **Date Invariants**:
   - `LeaveRequest.EndDate >= LeaveRequest.StartDate`.
   - `Expense.ExpenseDate <= UtcToday`.

4. **Leave Policy and Overlap**:
   - Active leave requests (`Pending`, `Approved`) for the same employee cannot have overlapping dates.
   - When a leave request is approved, the inclusive calendar days (`DaysCount`) are deducted from `Employee.AnnualLeaveBalanceDays`.
   - Leave cancellation is permitted only while in `Pending` state.

5. **Task Lifecycle**:
   - Permitted transitions: `Todo` -> `InProgress` -> `InReview` -> `Done`.
   - Cancellation permitted from non-completed states.

6. **Delete Behavior**:
   - Restrict deletes across all audit-sensitive records (`LeaveRequest`, `Expense`, `Department`, `Employee`).
   - Soft deactivation (`IsActive = false`) is preferred for employees and users to preserve historical operational data.
