using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.UnitTests.Domain;

public class ExpenseTests
{
    private readonly Guid _employeeId = Guid.NewGuid();
    private readonly Guid _managerId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_InitializesPendingExpense()
    {
        // Arrange
        var expenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        const decimal amount = 154.50m;

        // Act
        var expense = new Expense(_employeeId, ExpenseCategory.Meals, amount, "Team lunch", expenseDate, "USD");

        // Assert
        Assert.Equal(_employeeId, expense.EmployeeId);
        Assert.Equal(ExpenseCategory.Meals, expense.Category);
        Assert.Equal(amount, expense.Amount);
        Assert.Equal("USD", expense.Currency);
        Assert.Equal(ExpenseStatus.Pending, expense.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10.50)]
    public void Constructor_WithNonPositiveAmount_ThrowsDomainException(decimal amount)
    {
        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Expense(_employeeId, ExpenseCategory.Travel, amount, "Taxi", DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Contains("must be greater than zero", ex.Message);
    }

    [Fact]
    public void Constructor_WithFutureDate_ThrowsDomainException()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() =>
            new Expense(_employeeId, ExpenseCategory.Travel, 50m, "Flight ticket", futureDate));

        Assert.Contains("cannot be in the future", ex.Message);
    }

    [Fact]
    public void Approve_ByManager_SetsStatusToApproved()
    {
        // Arrange
        var expense = new Expense(_employeeId, ExpenseCategory.Equipment, 200m, "Monitor", DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        expense.Approve(_managerId, "Budget verified");

        // Assert
        Assert.Equal(ExpenseStatus.Approved, expense.Status);
        Assert.Equal(_managerId, expense.ReviewedByEmployeeId);
        Assert.NotNull(expense.ReviewedAtUtc);
    }

    [Fact]
    public void Approve_BySelf_ThrowsDomainException()
    {
        // Arrange
        var expense = new Expense(_employeeId, ExpenseCategory.Equipment, 200m, "Monitor", DateOnly.FromDateTime(DateTime.UtcNow));

        // Act & Assert (self-approval must be forbidden)
        var ex = Assert.Throws<DomainException>(() => expense.Approve(_employeeId));
        Assert.Contains("not permitted to approve their own", ex.Message);
    }

    [Fact]
    public void Reject_WithoutReason_ThrowsDomainException()
    {
        // Arrange
        var expense = new Expense(_employeeId, ExpenseCategory.Equipment, 200m, "Monitor", DateOnly.FromDateTime(DateTime.UtcNow));

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => expense.Reject(_managerId, ""));
        Assert.Contains("rejection reason is required", ex.Message);
    }
}
