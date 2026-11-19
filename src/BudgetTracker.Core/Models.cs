namespace BudgetTracker.Core;

public enum TransactionType
{
    Expense,
    Income,
}

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } = TransactionType.Expense;
    public string Category { get; set; } = "Other";
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}

public record CategoryBudget(string Category, decimal MonthlyLimit);

public record CategorySpending(string Category, decimal Spent, decimal? Limit)
{
    public decimal? Remaining => Limit - Spent;
    public double PercentUsed => Limit is > 0 ? (double)(Spent / Limit.Value) * 100 : 0;
    public bool OverBudget => Limit is not null && Spent > Limit;
}

public record MonthSummary(int Year, int Month, decimal Income, decimal Expenses)
{
    public decimal Net => Income - Expenses;
    public string Label => new DateOnly(Year, Month, 1).ToString("MMM yyyy");
}

public static class Categories
{
    public static readonly string[] Expense =
        ["Rent", "Groceries", "Transportation", "Eating Out", "Utilities", "Phone & Internet", "Entertainment", "Shopping", "Health", "Other"];

    public static readonly string[] Income = ["Salary", "Freelance", "Student Aid", "Gift", "Other Income"];
}
