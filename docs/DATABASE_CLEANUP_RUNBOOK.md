# Operix — BusinessOperationsDb Cleanup Runbook & Safety Procedure

**Database Server:** `.\SQLEXPRESS`  
**Target Database:** `BusinessOperationsDb`  
**Service Account:** `NT Service\MSSQL$SQLEXPRESS`  
**Default Backup Destination:** `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\`  
**Safety Script:** `scripts/cleanup_BusinessOperationsDb.sql`  

---

## 1. Safety Architecture Overview

The database cleanup procedure was constructed under strict defensive engineering principles:

1. **Guarded Transactions:** Uses `SET XACT_ABORT ON`, `QUOTED_IDENTIFIER ON`, and standard ANSI SET options, wrapped in a comprehensive `BEGIN TRY ... BEGIN CATCH` block.
2. **Mandatory Dry-Run Default:** The script defines `@DryRun BIT = 1`. In this mode, all candidate staging, updates, deletes, row-count checks, and post-cleanup assertions execute inside a transaction, but the transaction is **unconditionally rolled back**.
3. **No Blind Allowlists:** Candidate rows are staged into temporary tables (`#CandidateUsers`, `#CandidateEmployees`, `#CandidateDepartments`, `#CandidateTasks`, `#CandidateLeaveRequests`, `#CandidateExpenses`) using verified positive test signatures:
   - Synthetic domains: `@test.com`, `@example.com`
   - Synthetic department naming prefixes: `BogusDept%`, `Dept_%`, `TaskDept_%`, `LeaveDept_%`, etc.
   - Synthetic tasks, leave requests, and expenses linked to test employee creators.
4. **Zero-Ambiguity Guard:** Precondition 9 verifies that no unclassified, ambiguous records exist. If even a single record cannot be positively categorized as legitimate or synthetic, execution halts immediately with an exception.
5. **Exact Row Count Assertions:** Every single DML statement asserts that `@@ROWCOUNT` equals the exact audited delta. Any deviation triggers an immediate rollback.
6. **Referential Integrity & Dependency Sequence:**
   - Waleed Abdullah and Zain Abbas are updated to `Operations` *first*, satisfying `FK_Employees_Departments_DepartmentId`.
   - Test expenses and leave requests (including the 86 test leaves and 65 test expenses reviewed by legitimate employees) are deleted *before* test employees, preventing `FK_Expenses_Employees_ReviewedByEmployeeId` and `FK_LeaveRequests_Employees_ReviewedByEmployeeId` `NO_ACTION` violations.
   - Post-cleanup assertions verify zero orphan foreign keys across all parent-child relationships.

---

## 2. Backup & Verification Procedures

### Step 2.1: Full Database Backup with Checksum

Execute in PowerShell or SSMS before executing destructive statements:

```sql
BACKUP DATABASE [BusinessOperationsDb]
TO DISK = 'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\BusinessOperationsDb_PreCleanup_Backup.bak'
WITH FORMAT, INIT, CHECKSUM,
NAME = 'BusinessOperationsDb Pre-Cleanup Full Backup';
```

### Step 2.2: Backup Verification Procedure

Run immediately after backup creation to guarantee backup set validity and page checksum integrity:

```sql
RESTORE VERIFYONLY
FROM DISK = 'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\BusinessOperationsDb_PreCleanup_Backup.bak'
WITH CHECKSUM;
```

Expected output:
```
The backup set on file 1 is valid.
```

To view the backup header metadata:

```sql
RESTORE HEADERONLY
FROM DISK = 'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\BusinessOperationsDb_PreCleanup_Backup.bak';
```

### Step 2.3: Emergency Restore Procedure (If Ever Needed)

```sql
USE master;
ALTER DATABASE [BusinessOperationsDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE [BusinessOperationsDb]
FROM DISK = 'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\BusinessOperationsDb_PreCleanup_Backup.bak'
WITH REPLACE, CHECKSUM;

ALTER DATABASE [BusinessOperationsDb] SET MULTI_USER;
```

---

## 3. Change Manifest

| Target Table | Action | Candidate Rows | Starting Count | Expected Final Count | Verification Rule |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **Employees** | `UPDATE` | 2 | 815 | 815 (interim) | Reassign `DepartmentId` to `Operations` (`09bb9a8f-a907-424b-b5be-41f6c63e5426`) for Waleed Abdullah & Zain Abbas. |
| **Expenses** | `DELETE` | 129 | 131 | 2 | Submitter `EmployeeId` is a test employee (`@test.com`/`@example.com`). Preserves 2 legitimate expenses. |
| **LeaveRequests** | `DELETE` | 163 | 166 | 3 | Submitter `EmployeeId` is a test employee (`@test.com`/`@example.com`). Preserves 3 legitimate leave requests. |
| **TaskItems** | `DELETE` | 120 | 126 | 6 | Creator `CreatedByEmployeeId` is a test employee (`@test.com`/`@example.com`). Preserves 6 user/manager tasks. |
| **Employees** | `DELETE` | 810 | 815 | 5 | Synthetic test employees (`@test.com`/`@example.com`). Preserves Admin, Manager, Employee, Waleed, Zain. |
| **Users** | `DELETE` | 978 | 981 | 3 | Synthetic test users (`@test.com`/`@example.com`). Preserves Admin, Manager, Employee user logins. |
| **Departments** | `DELETE` | 879 | 885 | 6 | Synthetic test departments matching test prefixes. Preserves Operations, Engineering, Finance, Engineer, IT, fas. |

---

## 4. Preserved Business Entities Summary

### A. Preserved User Accounts (`dbo.Users`)
1. `admin@operix.com` (`8d5e6085-e35f-40e1-8651-8fb3a68a886a`) — Admin (Role 3)
2. `manager@operix.com` (`9449fadc-1a28-4fd2-bdac-1246e940b457`) — Manager (Role 2)
3. `employee@operix.com` (`27d2e04f-afe1-4a86-9c92-5636b9416fd2`) — Employee (Role 1)

### B. Preserved Staff Profiles (`dbo.Employees`)
1. `admin@operix.com` (`83dac036-9936-4349-b748-d160b2c04dcc`) — Admin User
2. `manager@operix.com` (`c996918c-7956-46e7-8d26-918b3fa83462`) — Sarah Jenkins
3. `employee@operix.com` (`0db84f6c-6fe0-4105-aaee-3ce41541b830`) — Alex Rivera
4. `itswaleedabdullah@gmail.com` (`297644f3-2292-483b-85b8-0ce44cef85ca`) — Waleed Abdullah (Reassigned to `Operations`)
5. `zain@gmail.com` (`9e1db608-c0ed-4e16-88bb-88d10b693dca`) — Zain Abbas (Reassigned to `Operations`)

### C. Preserved Departments (`dbo.Departments`)
1. `09bb9a8f-a907-424b-b5be-41f6c63e5426` — **Operations** (Manager: Sarah Jenkins)
2. `71b8dc76-bb14-4d06-a534-dab3d2eaf841` — **Engineering**
3. `dfe4b17f-e3a0-4d88-a8bc-5c9cd8af2673` — **Finance**
4. `9ffd94f7-db0e-4772-a975-43fa957a49c6` — **Engineer**
5. `dc5869d8-96cb-43dc-b372-08e9690b6d49` — **IT**
6. `b80e9eb6-df45-46f9-adad-28a7ef7208f9` — **fas**

### D. Preserved Tasks (`dbo.TaskItems`)
1. `2947f998-f0e0-4115-a6b6-950d4bbbca9f` — **fsd** (Created by Admin User, Unassigned)
2. `4541cd6a-fdf4-4229-813f-01866ffa0e7d` — **do fyp** (Created by Sarah Jenkins, Assigned to Waleed Abdullah)
3. `a679c377-6888-4523-a7cf-130817481193` — **Sjwdj** (Created by Alex Rivera, Unassigned)
4. `15a5eb93-f04f-4700-a4dd-f56765ce7cd1` — **asda** (Created by Admin User, Assigned to Waleed Abdullah)
5. `4bfebca0-f716-43f1-b888-e8b424c3f8e7` — **civil** (Created by Admin User, Assigned to Waleed Abdullah)
6. `307374b3-dafb-4a63-914c-159221f54fcd` — **website** (Created by Sarah Jenkins, Assigned to Zain Abbas)

### E. Preserved Leave Requests (`dbo.LeaveRequests`)
1. `4a1bdc8a-5903-4685-8d57-61bfe8af7461` — "i am boss" (Admin User, Reviewed by Sarah Jenkins, Approved)
2. `7958c0c7-2698-406c-9f14-7b0d78cb6f81` — "Recovery verification" (Waleed Abdullah, Reviewed by Admin User, Approved)
3. `02bde388-d316-4c8e-9328-fdf5a7c40125` — "Medical appointment" (Waleed Abdullah, Reviewed by Admin User, Rejected)

### F. Preserved Expenses (`dbo.Expenses`)
1. `9bfbf60b-6a01-4dfb-a106-bd01b9a20540` — "flight ticket" ($50.00, Admin User, Reviewed by Sarah Jenkins, Approved)
2. `8d2b1bdc-2e62-414d-8262-49d4c45d6f27` — "asdas" ($50.00, Admin User, Reviewed by Sarah Jenkins, Approved)
