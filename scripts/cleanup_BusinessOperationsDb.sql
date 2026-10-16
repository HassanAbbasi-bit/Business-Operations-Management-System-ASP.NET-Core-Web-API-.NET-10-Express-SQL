/*
================================================================================
Operix - BusinessOperationsDb Production-Quality Guarded Cleanup Procedure
Server: .\SQLEXPRESS | Target Database: BusinessOperationsDb
--------------------------------------------------------------------------------
SAFETY GUARANTEES:
1. SET XACT_ABORT ON and explicit TRY/CATCH block.
2. Guarded by @DryRun bit (DEFAULT = 1). Must be explicitly set to 0 to commit.
3. Preconditions verify live starting row counts, entity existence, and 0 ambiguous records.
4. Each step verifies @@ROWCOUNT against audited targets; throws on discrepancy.
5. Post-cleanup assertions verify exact final row counts and referential integrity.
6. Test-generated reviewer references on expenses and leave requests are purged 
   prior to test employee deletion, avoiding FK constraint violations.
7. Waleed Abdullah and Zain Abbas are safely reassigned to 'Operations' department
   before synthetic departments are deleted.
8. If ANY error occurs or if @DryRun = 1, TRANSACTION IS ROLLED BACK.
================================================================================
*/

SET XACT_ABORT ON;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

-- ============================================================================
-- EXECUTION MODE CONFIGURATION
-- Set @DryRun = 1 for safe simulation with mandatory ROLLBACK.
-- Set @DryRun = 0 ONLY AFTER explicit human approval and verified backup.
-- ============================================================================
DECLARE @DryRun BIT = 1;

PRINT '======================================================================';
PRINT 'Operix BusinessOperationsDb Guarded Cleanup Procedure';
PRINT 'Execution Mode: ' + CASE WHEN @DryRun = 1 THEN 'DRY-RUN (Always Rolls Back)' ELSE 'PRODUCTION COMMIT' END;
PRINT 'Timestamp (UTC): ' + CONVERT(VARCHAR(30), SYSUTCDATETIME(), 126);
PRINT '======================================================================';

-- ----------------------------------------------------------------------------
-- Precondition 0: Verify target database context
-- ----------------------------------------------------------------------------
IF DB_NAME() <> 'BusinessOperationsDb'
BEGIN
    DECLARE @ContextErr NVARCHAR(200) = 'ABORT: This script must ONLY be executed against [BusinessOperationsDb]. Current DB: ' + DB_NAME();
    THROW 51000, @ContextErr, 1;
END;

-- ----------------------------------------------------------------------------
-- Precondition 1: Verify 'Operations' department exists exactly once
-- ----------------------------------------------------------------------------
DECLARE @OperationsDeptId UNIQUEIDENTIFIER;
SELECT @OperationsDeptId = Id 
FROM Departments 
WHERE Name = 'Operations';

IF @OperationsDeptId IS NULL
BEGIN
    THROW 51001, 'ABORT: Operations department not found.', 1;
END;

IF (SELECT COUNT(*) FROM Departments WHERE Name = 'Operations') <> 1
BEGIN
    THROW 51002, 'ABORT: Multiple departments named Operations found.', 1;
END;

PRINT '>> Precondition 1 Passed: Operations department verified (ID: ' + CAST(@OperationsDeptId AS VARCHAR(36)) + ').';

-- ----------------------------------------------------------------------------
-- Precondition 2: Verify legitimate employees exist
-- ----------------------------------------------------------------------------
DECLARE @WaleedId UNIQUEIDENTIFIER, @ZainId UNIQUEIDENTIFIER;
SELECT @WaleedId = Id FROM Employees WHERE Email = 'itswaleedabdullah@gmail.com';
SELECT @ZainId = Id FROM Employees WHERE Email = 'zain@gmail.com';

IF @WaleedId IS NULL OR @ZainId IS NULL
BEGIN
    THROW 51003, 'ABORT: Expected employees Waleed Abdullah or Zain Abbas not found.', 1;
END;

IF (SELECT COUNT(*) FROM Employees WHERE Email IN ('admin@operix.com', 'manager@operix.com', 'employee@operix.com', 'itswaleedabdullah@gmail.com', 'zain@gmail.com')) <> 5
BEGIN
    THROW 51004, 'ABORT: Missing one or more of the 5 legitimate employee records.', 1;
END;

PRINT '>> Precondition 2 Passed: 5 legitimate employees verified.';

-- ----------------------------------------------------------------------------
-- Precondition 3: Verify legitimate users exist
-- ----------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM Users WHERE Email IN ('admin@operix.com', 'manager@operix.com', 'employee@operix.com')) <> 3
BEGIN
    THROW 51005, 'ABORT: Missing one or more of the 3 legitimate user records.', 1;
END;

PRINT '>> Precondition 3 Passed: 3 legitimate user accounts verified.';

-- ----------------------------------------------------------------------------
-- Precondition 4: Verify legitimate departments exist
-- ----------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM Departments WHERE Id IN (
    '09bb9a8f-a907-424b-b5be-41f6c63e5426', -- Operations
    '71b8dc76-bb14-4d06-a534-dab3d2eaf841', -- Engineering
    'dfe4b17f-e3a0-4d88-a8bc-5c9cd8af2673', -- Finance
    '9ffd94f7-db0e-4772-a975-43fa957a49c6', -- Engineer
    'dc5869d8-96cb-43dc-b372-08e9690b6d49', -- IT
    'b80e9eb6-df45-46f9-adad-28a7ef7208f9'  -- fas
)) <> 6
BEGIN
    THROW 51006, 'ABORT: Missing one or more of the 6 legitimate department records.', 1;
END;

PRINT '>> Precondition 4 Passed: 6 legitimate departments verified.';

-- ----------------------------------------------------------------------------
-- Precondition 5: Verify the 6 preserved tasks and their relationships
-- ----------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM TaskItems WHERE Id IN (
    '2947f998-f0e0-4115-a6b6-950d4bbbca9f', -- fsd (Admin)
    '4541cd6a-fdf4-4229-813f-01866ffa0e7d', -- do fyp (Sarah -> Waleed)
    'a679c377-6888-4523-a7cf-130817481193', -- Sjwdj (Alex)
    '15a5eb93-f04f-4700-a4dd-f56765ce7cd1', -- asda (Admin -> Waleed)
    '4bfebca0-f716-43f1-b888-e8b424c3f8e7', -- civil (Admin -> Waleed)
    '307374b3-dafb-4a63-914c-159221f54fcd'  -- website (Sarah -> Zain)
)) <> 6
BEGIN
    THROW 51007, 'ABORT: Missing one or more of the 6 preserved tasks.', 1;
END;

-- Ensure none of the 6 tasks reference non-legitimate employees
IF EXISTS (
    SELECT 1 FROM TaskItems t
    WHERE t.Id IN (
        '2947f998-f0e0-4115-a6b6-950d4bbbca9f',
        '4541cd6a-fdf4-4229-813f-01866ffa0e7d',
        'a679c377-6888-4523-a7cf-130817481193',
        '15a5eb93-f04f-4700-a4dd-f56765ce7cd1',
        '4bfebca0-f716-43f1-b888-e8b424c3f8e7',
        '307374b3-dafb-4a63-914c-159221f54fcd'
    )
    AND (
        t.CreatedByEmployeeId NOT IN (
            SELECT Id FROM Employees WHERE Email IN ('admin@operix.com', 'manager@operix.com', 'employee@operix.com', 'itswaleedabdullah@gmail.com', 'zain@gmail.com')
        )
        OR (
            t.AssignedToEmployeeId IS NOT NULL 
            AND t.AssignedToEmployeeId NOT IN (
                SELECT Id FROM Employees WHERE Email IN ('admin@operix.com', 'manager@operix.com', 'employee@operix.com', 'itswaleedabdullah@gmail.com', 'zain@gmail.com')
            )
        )
    )
)
BEGIN
    THROW 51008, 'ABORT: One or more preserved tasks reference non-legitimate employee accounts.', 1;
END;

PRINT '>> Precondition 5 Passed: 6 preserved tasks and relationships verified.';

-- ----------------------------------------------------------------------------
-- Precondition 6: Verify legitimate leave requests (3) and expenses (2)
-- ----------------------------------------------------------------------------
IF (SELECT COUNT(*) FROM LeaveRequests WHERE Id IN (
    '4a1bdc8a-5903-4685-8d57-61bfe8af7461',
    '7958c0c7-2698-406c-9f14-7b0d78cb6f81',
    '02bde388-d316-4c8e-9328-fdf5a7c40125'
)) <> 3
BEGIN
    THROW 51009, 'ABORT: Missing one or more of the 3 preserved leave requests.', 1;
END;

IF (SELECT COUNT(*) FROM Expenses WHERE Id IN (
    '9bfbf60b-6a01-4dfb-a106-bd01b9a20540',
    '8d2b1bdc-2e62-414d-8262-49d4c45d6f27'
)) <> 2
BEGIN
    THROW 51010, 'ABORT: Missing one or more of the 2 preserved expenses.', 1;
END;

PRINT '>> Precondition 6 Passed: Legitimate leave requests and expenses verified.';

-- ----------------------------------------------------------------------------
-- Precondition 7: Verify live starting counts match audited baseline
-- ----------------------------------------------------------------------------
DECLARE @StartUsers INT, @StartEmployees INT, @StartDepartments INT, 
        @StartTasks INT, @StartLeaves INT, @StartExpenses INT;

SELECT @StartUsers = COUNT(*) FROM Users;
SELECT @StartEmployees = COUNT(*) FROM Employees;
SELECT @StartDepartments = COUNT(*) FROM Departments;
SELECT @StartTasks = COUNT(*) FROM TaskItems;
SELECT @StartLeaves = COUNT(*) FROM LeaveRequests;
SELECT @StartExpenses = COUNT(*) FROM Expenses;

IF @StartUsers <> 981 OR @StartEmployees <> 815 OR @StartDepartments <> 885 OR
   @StartTasks <> 126 OR @StartLeaves <> 166 OR @StartExpenses <> 131
BEGIN
    THROW 51011, 'ABORT: Live starting row counts do not match audited baseline. Possible concurrent modifications.', 1;
END;

PRINT '>> Precondition 7 Passed: Starting row counts match audited baseline (Users=981, Emp=815, Dept=885, Tasks=126, Leaves=166, Exp=131).';

-- ----------------------------------------------------------------------------
-- Stage candidate IDs based on verified positive test patterns (NOT IN allowlists avoided)
-- ----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#CandidateUsers') IS NOT NULL DROP TABLE #CandidateUsers;
IF OBJECT_ID('tempdb..#CandidateEmployees') IS NOT NULL DROP TABLE #CandidateEmployees;
IF OBJECT_ID('tempdb..#CandidateDepartments') IS NOT NULL DROP TABLE #CandidateDepartments;
IF OBJECT_ID('tempdb..#CandidateTasks') IS NOT NULL DROP TABLE #CandidateTasks;
IF OBJECT_ID('tempdb..#CandidateLeaveRequests') IS NOT NULL DROP TABLE #CandidateLeaveRequests;
IF OBJECT_ID('tempdb..#CandidateExpenses') IS NOT NULL DROP TABLE #CandidateExpenses;

CREATE TABLE #CandidateUsers (Id UNIQUEIDENTIFIER PRIMARY KEY);
CREATE TABLE #CandidateEmployees (Id UNIQUEIDENTIFIER PRIMARY KEY);
CREATE TABLE #CandidateDepartments (Id UNIQUEIDENTIFIER PRIMARY KEY);
CREATE TABLE #CandidateTasks (Id UNIQUEIDENTIFIER PRIMARY KEY);
CREATE TABLE #CandidateLeaveRequests (Id UNIQUEIDENTIFIER PRIMARY KEY);
CREATE TABLE #CandidateExpenses (Id UNIQUEIDENTIFIER PRIMARY KEY);

-- Classify Users: created by tests with @test.com or @example.com
INSERT INTO #CandidateUsers (Id)
SELECT Id FROM Users
WHERE Email LIKE '%@test.com' OR Email LIKE '%@example.com';

-- Classify Employees: created by tests with @test.com or @example.com
INSERT INTO #CandidateEmployees (Id)
SELECT Id FROM Employees
WHERE Email LIKE '%@test.com' OR Email LIKE '%@example.com';

-- Classify Departments: created with known integration test naming patterns
INSERT INTO #CandidateDepartments (Id)
SELECT Id FROM Departments
WHERE (
    Name LIKE 'BogusDept%' OR
    Name LIKE 'Dept\_%' ESCAPE '\' OR
    Name LIKE 'TaskDept\_%' ESCAPE '\' OR
    Name LIKE 'LeaveDept\_%' ESCAPE '\' OR
    Name LIKE 'ExpDept\_%' ESCAPE '\' OR
    Name LIKE 'DashDept\_%' ESCAPE '\' OR
    Name LIKE 'Engineering\_%' ESCAPE '\' OR
    Name LIKE 'DuplicateDept\_%' ESCAPE '\' OR
    Name LIKE 'FinanceUpdated\_%' ESCAPE '\' OR
    Name LIKE 'OriginalDept%' OR
    Name LIKE 'ForbiddenDept\_%' ESCAPE '\' OR
    Name LIKE 'UpdatedDept%'
);

-- Classify Tasks: created by test employees
INSERT INTO #CandidateTasks (Id)
SELECT Id FROM TaskItems
WHERE CreatedByEmployeeId IN (SELECT Id FROM #CandidateEmployees);

-- Classify Leave Requests: submitted by test employees
INSERT INTO #CandidateLeaveRequests (Id)
SELECT Id FROM LeaveRequests
WHERE EmployeeId IN (SELECT Id FROM #CandidateEmployees);

-- Classify Expenses: submitted by test employees
INSERT INTO #CandidateExpenses (Id)
SELECT Id FROM Expenses
WHERE EmployeeId IN (SELECT Id FROM #CandidateEmployees);

-- ----------------------------------------------------------------------------
-- Precondition 8: Verify candidate counts match expected deltas exactly
-- ----------------------------------------------------------------------------
DECLARE @CandidateUsersCount INT = (SELECT COUNT(*) FROM #CandidateUsers);
DECLARE @CandidateEmployeesCount INT = (SELECT COUNT(*) FROM #CandidateEmployees);
DECLARE @CandidateDepartmentsCount INT = (SELECT COUNT(*) FROM #CandidateDepartments);
DECLARE @CandidateTasksCount INT = (SELECT COUNT(*) FROM #CandidateTasks);
DECLARE @CandidateLeavesCount INT = (SELECT COUNT(*) FROM #CandidateLeaveRequests);
DECLARE @CandidateExpensesCount INT = (SELECT COUNT(*) FROM #CandidateExpenses);

IF @CandidateUsersCount <> 978 OR @CandidateEmployeesCount <> 810 OR @CandidateDepartmentsCount <> 879 OR
   @CandidateTasksCount <> 120 OR @CandidateLeavesCount <> 163 OR @CandidateExpensesCount <> 129
BEGIN
    THROW 51012, 'ABORT: Candidate counts do not match expected cleanup deltas.', 1;
END;

PRINT '>> Precondition 8 Passed: Candidate counts verified (Users=978, Emp=810, Dept=879, Tasks=120, Leaves=163, Exp=129).';

-- ----------------------------------------------------------------------------
-- Precondition 9: Ensure ZERO ambiguous records exist (unclassified rows)
-- ----------------------------------------------------------------------------
DECLARE @AmbiguousUsers INT = (
    SELECT COUNT(*) FROM Users 
    WHERE Id NOT IN (SELECT Id FROM #CandidateUsers)
      AND Email NOT IN ('admin@operix.com', 'manager@operix.com', 'employee@operix.com')
);

DECLARE @AmbiguousEmployees INT = (
    SELECT COUNT(*) FROM Employees
    WHERE Id NOT IN (SELECT Id FROM #CandidateEmployees)
      AND Email NOT IN ('admin@operix.com', 'manager@operix.com', 'employee@operix.com', 'itswaleedabdullah@gmail.com', 'zain@gmail.com')
);

DECLARE @AmbiguousDepartments INT = (
    SELECT COUNT(*) FROM Departments
    WHERE Id NOT IN (SELECT Id FROM #CandidateDepartments)
      AND Id NOT IN (
          '09bb9a8f-a907-424b-b5be-41f6c63e5426', '71b8dc76-bb14-4d06-a534-dab3d2eaf841',
          'dfe4b17f-e3a0-4d88-a8bc-5c9cd8af2673', '9ffd94f7-db0e-4772-a975-43fa957a49c6',
          'dc5869d8-96cb-43dc-b372-08e9690b6d49', 'b80e9eb6-df45-46f9-adad-28a7ef7208f9'
      )
);

DECLARE @AmbiguousTasks INT = (
    SELECT COUNT(*) FROM TaskItems
    WHERE Id NOT IN (SELECT Id FROM #CandidateTasks)
      AND Id NOT IN (
          '2947f998-f0e0-4115-a6b6-950d4bbbca9f', '4541cd6a-fdf4-4229-813f-01866ffa0e7d',
          'a679c377-6888-4523-a7cf-130817481193', '15a5eb93-f04f-4700-a4dd-f56765ce7cd1',
          '4bfebca0-f716-43f1-b888-e8b424c3f8e7', '307374b3-dafb-4a63-914c-159221f54fcd'
      )
);

DECLARE @AmbiguousLeaves INT = (
    SELECT COUNT(*) FROM LeaveRequests
    WHERE Id NOT IN (SELECT Id FROM #CandidateLeaveRequests)
      AND Id NOT IN (
          '4a1bdc8a-5903-4685-8d57-61bfe8af7461', '7958c0c7-2698-406c-9f14-7b0d78cb6f81', '02bde388-d316-4c8e-9328-fdf5a7c40125'
      )
);

DECLARE @AmbiguousExpenses INT = (
    SELECT COUNT(*) FROM Expenses
    WHERE Id NOT IN (SELECT Id FROM #CandidateExpenses)
      AND Id NOT IN (
          '9bfbf60b-6a01-4dfb-a106-bd01b9a20540', '8d2b1bdc-2e62-414d-8262-49d4c45d6f27'
      )
);

IF @AmbiguousUsers > 0 OR @AmbiguousEmployees > 0 OR @AmbiguousDepartments > 0 OR
   @AmbiguousTasks > 0 OR @AmbiguousLeaves > 0 OR @AmbiguousExpenses > 0
BEGIN
    THROW 51013, 'ABORT: Ambiguous unclassified records detected. Halting cleanup for manual review.', 1;
END;

PRINT '>> Precondition 9 Passed: Zero ambiguous records detected. 100% of rows strictly classified.';

-- ============================================================================
-- GUARDED TRANSACTION EXECUTION
-- ============================================================================
BEGIN TRY
    BEGIN TRANSACTION;

    -- Step 1: Reassign Waleed Abdullah and Zain Abbas to 'Operations'
    PRINT 'Step 1: Reassigning Waleed Abdullah and Zain Abbas to Operations department...';
    UPDATE Employees
    SET DepartmentId = @OperationsDeptId,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE Email IN ('itswaleedabdullah@gmail.com', 'zain@gmail.com');

    DECLARE @Step1Rows INT = @@ROWCOUNT;
    IF @Step1Rows <> 2
    BEGIN
        THROW 51020, 'Step 1 Failed: Expected exactly 2 employee department reassignments.', 1;
    END;
    PRINT '>> Step 1 Completed: ' + CAST(@Step1Rows AS VARCHAR(10)) + ' employees successfully reassigned to Operations.';

    -- Step 2: Delete candidate expenses (including test reviewer references)
    PRINT 'Step 2: Deleting synthetic candidate expenses...';
    DELETE FROM Expenses
    WHERE Id IN (SELECT Id FROM #CandidateExpenses);

    DECLARE @Step2Rows INT = @@ROWCOUNT;
    IF @Step2Rows <> 129
    BEGIN
        THROW 51021, 'Step 2 Failed: Expected exactly 129 expenses deleted.', 1;
    END;
    PRINT '>> Step 2 Completed: ' + CAST(@Step2Rows AS VARCHAR(10)) + ' synthetic expenses deleted.';

    -- Step 3: Delete candidate leave requests (including test reviewer references)
    PRINT 'Step 3: Deleting synthetic candidate leave requests...';
    DELETE FROM LeaveRequests
    WHERE Id IN (SELECT Id FROM #CandidateLeaveRequests);

    DECLARE @Step3Rows INT = @@ROWCOUNT;
    IF @Step3Rows <> 163
    BEGIN
        THROW 51022, 'Step 3 Failed: Expected exactly 163 leave requests deleted.', 1;
    END;
    PRINT '>> Step 3 Completed: ' + CAST(@Step3Rows AS VARCHAR(10)) + ' synthetic leave requests deleted.';

    -- Step 4: Delete candidate tasks
    PRINT 'Step 4: Deleting synthetic candidate tasks...';
    DELETE FROM TaskItems
    WHERE Id IN (SELECT Id FROM #CandidateTasks);

    DECLARE @Step4Rows INT = @@ROWCOUNT;
    IF @Step4Rows <> 120
    BEGIN
        THROW 51023, 'Step 4 Failed: Expected exactly 120 tasks deleted.', 1;
    END;
    PRINT '>> Step 4 Completed: ' + CAST(@Step4Rows AS VARCHAR(10)) + ' synthetic tasks deleted.';

    -- Step 5: Delete candidate employees
    PRINT 'Step 5: Deleting synthetic candidate employees...';
    DELETE FROM Employees
    WHERE Id IN (SELECT Id FROM #CandidateEmployees);

    DECLARE @Step5Rows INT = @@ROWCOUNT;
    IF @Step5Rows <> 810
    BEGIN
        THROW 51024, 'Step 5 Failed: Expected exactly 810 employees deleted.', 1;
    END;
    PRINT '>> Step 5 Completed: ' + CAST(@Step5Rows AS VARCHAR(10)) + ' synthetic employees deleted.';

    -- Step 6: Delete candidate users
    PRINT 'Step 6: Deleting synthetic candidate users...';
    DELETE FROM Users
    WHERE Id IN (SELECT Id FROM #CandidateUsers);

    DECLARE @Step6Rows INT = @@ROWCOUNT;
    IF @Step6Rows <> 978
    BEGIN
        THROW 51025, 'Step 6 Failed: Expected exactly 978 users deleted.', 1;
    END;
    PRINT '>> Step 6 Completed: ' + CAST(@Step6Rows AS VARCHAR(10)) + ' synthetic users deleted.';

    -- Step 7: Delete candidate departments
    PRINT 'Step 7: Deleting synthetic candidate departments...';
    DELETE FROM Departments
    WHERE Id IN (SELECT Id FROM #CandidateDepartments);

    DECLARE @Step7Rows INT = @@ROWCOUNT;
    IF @Step7Rows <> 879
    BEGIN
        THROW 51026, 'Step 7 Failed: Expected exactly 879 departments deleted.', 1;
    END;
    PRINT '>> Step 7 Completed: ' + CAST(@Step7Rows AS VARCHAR(10)) + ' synthetic departments deleted.';

    -- ========================================================================
    -- POST-CLEANUP ASSERTIONS & REFERENTIAL INTEGRITY CHECKS
    -- ========================================================================
    PRINT 'Checking post-cleanup assertions and referential integrity...';

    DECLARE @FinalUsers INT = (SELECT COUNT(*) FROM Users);
    DECLARE @FinalEmployees INT = (SELECT COUNT(*) FROM Employees);
    DECLARE @FinalDepartments INT = (SELECT COUNT(*) FROM Departments);
    DECLARE @FinalTasks INT = (SELECT COUNT(*) FROM TaskItems);
    DECLARE @FinalLeaves INT = (SELECT COUNT(*) FROM LeaveRequests);
    DECLARE @FinalExpenses INT = (SELECT COUNT(*) FROM Expenses);

    IF @FinalUsers <> 3 OR @FinalEmployees <> 5 OR @FinalDepartments <> 6 OR
       @FinalTasks <> 6 OR @FinalLeaves <> 3 OR @FinalExpenses <> 2
    BEGIN
        THROW 51030, 'Post-Assertion Failed: Final table counts do not match target clean baseline.', 1;
    END;

    -- Integrity Check 1: Orphan employee department references
    IF EXISTS (SELECT 1 FROM Employees e WHERE NOT EXISTS (SELECT 1 FROM Departments d WHERE d.Id = e.DepartmentId))
    BEGIN
        THROW 51031, 'Post-Assertion Failed: Orphan DepartmentId found in Employees.', 1;
    END;

    -- Integrity Check 2: Waleed and Zain assigned to Operations
    IF (SELECT COUNT(*) FROM Employees WHERE Email IN ('itswaleedabdullah@gmail.com', 'zain@gmail.com') AND DepartmentId = @OperationsDeptId) <> 2
    BEGIN
        THROW 51032, 'Post-Assertion Failed: Waleed Abdullah and Zain Abbas are not in Operations department.', 1;
    END;

    -- Integrity Check 3: Orphan task creator or assignee references
    IF EXISTS (SELECT 1 FROM TaskItems t WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = t.CreatedByEmployeeId))
    BEGIN
        THROW 51033, 'Post-Assertion Failed: Orphan CreatedByEmployeeId found in TaskItems.', 1;
    END;

    IF EXISTS (SELECT 1 FROM TaskItems t WHERE t.AssignedToEmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = t.AssignedToEmployeeId))
    BEGIN
        THROW 51034, 'Post-Assertion Failed: Orphan AssignedToEmployeeId found in TaskItems.', 1;
    END;

    -- Integrity Check 4: Orphan leave request submitter or reviewer references
    IF EXISTS (SELECT 1 FROM LeaveRequests lr WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = lr.EmployeeId))
    BEGIN
        THROW 51035, 'Post-Assertion Failed: Orphan EmployeeId found in LeaveRequests.', 1;
    END;

    IF EXISTS (SELECT 1 FROM LeaveRequests lr WHERE lr.ReviewedByEmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = lr.ReviewedByEmployeeId))
    BEGIN
        THROW 51036, 'Post-Assertion Failed: Orphan ReviewedByEmployeeId found in LeaveRequests.', 1;
    END;

    -- Integrity Check 5: Orphan expense submitter or reviewer references
    IF EXISTS (SELECT 1 FROM Expenses ex WHERE NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = ex.EmployeeId))
    BEGIN
        THROW 51037, 'Post-Assertion Failed: Orphan EmployeeId found in Expenses.', 1;
    END;

    IF EXISTS (SELECT 1 FROM Expenses ex WHERE ex.ReviewedByEmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Employees e WHERE e.Id = ex.ReviewedByEmployeeId))
    BEGIN
        THROW 51038, 'Post-Assertion Failed: Orphan ReviewedByEmployeeId found in Expenses.', 1;
    END;

    PRINT '>> All post-cleanup assertions and referential integrity checks PASSED.';
    PRINT '----------------------------------------------------------------------';
    PRINT 'FINAL CHANGE MANIFEST SUMMARY:';
    PRINT '  Table           | Candidate Rows | Action | Initial Count | Target Count';
    PRINT '  ----------------+----------------+--------+---------------+-------------';
    PRINT '  Employees       | 2              | UPDATE | 815           | 5           ';
    PRINT '  Expenses        | 129            | DELETE | 131           | 2           ';
    PRINT '  LeaveRequests   | 163            | DELETE | 166           | 3           ';
    PRINT '  TaskItems       | 120            | DELETE | 126           | 6           ';
    PRINT '  Employees       | 810            | DELETE | 815           | 5           ';
    PRINT '  Users           | 978            | DELETE | 981           | 3           ';
    PRINT '  Departments     | 879            | DELETE | 885           | 6           ';
    PRINT '----------------------------------------------------------------------';

    IF @DryRun = 1
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT '>> [DRY-RUN RESULT]: VALIDATION SUCCESSFUL. TRANSACTION ROLLED BACK AS DESIGNED.';
        PRINT '>> Zero records were modified or deleted permanently.';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT '>> [PRODUCTION COMMIT RESULT]: DATABASE CLEANUP COMMITTED SUCCESSFULLY.';
    END;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    DECLARE @ErrLine INT = ERROR_LINE();

    PRINT '**********************************************************************';
    PRINT 'ERROR ENCOUNTERED ON LINE ' + CAST(@ErrLine AS VARCHAR(10)) + '! TRANSACTION ROLLED BACK.';
    PRINT 'Error Message: ' + @ErrMsg;
    PRINT '**********************************************************************';

    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
