using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.UnitTests.Domain;

public class LeaveRequestTests
{
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_InitializesPendingRequest()
    {
        // Arrange
        var startDate = new DateOnly(2026, 11, 1);
        var endDate = new DateOnly(2026, 11, 5);
        const string reason = "Family vacation";

        // Act
        var request = new LeaveRequest(_employeeId, startDate, endDate, reason);

        // Assert
        Assert.Equal(_employeeId, request.EmployeeId);
        Assert.Equal(startDate, request.StartDate);
        Assert.Equal(endDate, request.EndDate);
        Assert.Equal(reason, request.Reason);
        Assert.Equal(LeaveStatus.Pending, request.Status);
        Assert.Equal(5, request.DaysCount);
    }

    [Fact]
    public void Constructor_WithEndDateBeforeStartDate_ThrowsDomainException()
    {
        // Arrange
        var startDate = new DateOnly(2026, 11, 5);
        var endDate = new DateOnly(2026, 11, 1);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new LeaveRequest(_employeeId, startDate, endDate, "Invalid dates"));

        Assert.Contains("cannot precede start date", ex.Message);
    }

    [Fact]
    public void Approve_ByManager_SetsStatusToApproved()
    {
        // Arrange
        var request = new LeaveRequest(_employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), "Sick leave");

        // Act
        request.Approve(_managerId, "Approved by manager");

        // Assert
        Assert.Equal(LeaveStatus.Approved, request.Status);
        Assert.Equal(_managerId, request.ReviewedByEmployeeId);
        Assert.NotNull(request.ReviewedAtUtc);
        Assert.Equal("Approved by manager", request.ReviewNotes);
    }

    [Fact]
    public void Approve_ByRequestingEmployee_ThrowsDomainException()
    {
        // Arrange
        var request = new LeaveRequest(_employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), "Personal");

        // Act & Assert (self-approval must be forbidden)
        var ex = Assert.Throws<DomainException>(() => request.Approve(_employeeId));
        Assert.Contains("not permitted to approve their own", ex.Message);
    }

    [Fact]
    public void Reject_ByManager_SetsStatusToRejected()
    {
        // Arrange
        var request = new LeaveRequest(_employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), "Vacation");

        // Act
        request.Reject(_managerId, "Peak operational period");

        // Assert
        Assert.Equal(LeaveStatus.Rejected, request.Status);
        Assert.Equal(_managerId, request.ReviewedByEmployeeId);
        Assert.Equal("Peak operational period", request.ReviewNotes);
    }

    [Fact]
    public void Reject_WithoutReason_ThrowsDomainException()
    {
        // Arrange
        var request = new LeaveRequest(_employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), "Vacation");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => request.Reject(_managerId, "   "));
        Assert.Contains("rejection reason is required", ex.Message);
    }

    [Fact]
    public void Cancel_ByRequestingEmployee_SetsStatusToCancelled()
    {
        // Arrange
        var request = new LeaveRequest(_employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), "Vacation");

        // Act
        request.Cancel(_employeeId);

        // Assert
        Assert.Equal(LeaveStatus.Cancelled, request.Status);
    }

    [Fact]
    public void Cancel_ByAnotherEmployee_ThrowsDomainException()
    {
        // Arrange
        var request = new LeaveRequest(_employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), "Vacation");

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => request.Cancel(Guid.NewGuid()));
        Assert.Contains("Only the employee who submitted the request may cancel", ex.Message);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsDomainException()
    {
        // Arrange
        var request = new LeaveRequest(_employeeId, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2), "Vacation");
        request.Approve(_managerId);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => request.Approve(_managerId));
        Assert.Contains("Only pending requests can be transitioned", ex.Message);
    }
}
