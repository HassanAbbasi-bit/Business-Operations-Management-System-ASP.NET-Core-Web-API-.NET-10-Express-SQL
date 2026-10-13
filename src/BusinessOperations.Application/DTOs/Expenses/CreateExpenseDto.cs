using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.DTOs.Expenses;

public class CreateExpenseDto
{
    public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public DateOnly ExpenseDate { get; set; }
}
