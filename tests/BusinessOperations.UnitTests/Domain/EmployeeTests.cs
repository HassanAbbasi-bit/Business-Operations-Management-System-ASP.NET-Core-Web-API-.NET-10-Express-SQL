using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.UnitTests.Domain;

public class EmployeeTests
{
    private readonly Guid _deptId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_InitializesEmployeeWithDefaultLeaveBalance()
    {
        // Act
        var employee = new Employee("Alice", "Smith", "alice@example.com", _deptId, new DateOnly(2025, 1, 15));

        // Assert
        Assert.Equal("Alice", employee.FirstName);
        Assert.Equal("Smith", employee.LastName);
        Assert.Equal("Alice Smith", employee.FullName);
        Assert.Equal("alice@example.com", employee.Email);
        Assert.Equal(20, employee.AnnualLeaveBalanceDays);
        Assert.True(employee.IsActive);
    }

    [Fact]
    public void DeductLeaveDays_WithSufficientBalance_ReducesBalance()
    {
        // Arrange
        var employee = new Employee("Bob", "Jones", "bob@example.com", _deptId, new DateOnly(2025, 1, 15), annualLeaveBalanceDays: 20);

        // Act
        employee.DeductLeaveDays(5);

        // Assert
        Assert.Equal(15, employee.AnnualLeaveBalanceDays);
    }

    [Fact]
    public void DeductLeaveDays_WithInsufficientBalance_ThrowsDomainException()
    {
        // Arrange
        var employee = new Employee("Bob", "Jones", "bob@example.com", _deptId, new DateOnly(2025, 1, 15), annualLeaveBalanceDays: 3);

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => employee.DeductLeaveDays(5));
        Assert.Contains("Insufficient leave balance", ex.Message);
    }

    [Fact]
    public void CreditLeaveDays_WithValidDays_IncreasesBalance()
    {
        // Arrange
        var employee = new Employee("Bob", "Jones", "bob@example.com", _deptId, new DateOnly(2025, 1, 15), annualLeaveBalanceDays: 10);

        // Act
        employee.CreditLeaveDays(5);

        // Assert
        Assert.Equal(15, employee.AnnualLeaveBalanceDays);
    }

    [Fact]
    public void AssignManager_ToSelf_ThrowsDomainException()
    {
        // Arrange
        var employee = new Employee("Bob", "Jones", "bob@example.com", _deptId, new DateOnly(2025, 1, 15));

        // Act & Assert (self-reporting must be forbidden)
        var ex = Assert.Throws<DomainException>(() => employee.AssignManager(employee.Id));
        Assert.Contains("cannot report to themselves", ex.Message);
    }
}
