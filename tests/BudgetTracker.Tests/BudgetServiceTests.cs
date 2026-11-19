using BudgetTracker.Core;

namespace BudgetTracker.Tests;

public class BudgetServiceTests
{
    private static Transaction Expense(string desc, decimal amount, string category, int day = 10, int month = 3) =>
        new() { Description = desc, Amount = amount, Category = category, Date = new DateOnly(2026, month, day) };

    private static Transaction Income(decimal amount, int month = 3) =>
        new() { Description = "Pay", Amount = amount, Type = TransactionType.Income, Category = "Salary", Date = new DateOnly(2026, month, 1) };

    [Fact]
    public void Summary_totals_income_and_expenses_for_the_month_only()
    {
        var service = new BudgetService();
        service.Add(Income(2000));
        service.Add(Expense("Rent", 900, "Rent"));
        service.Add(Expense("Groceries", 100, "Groceries"));
        service.Add(Expense("Last month", 500, "Rent", month: 2));

        var summary = service.Summary(2026, 3);

        Assert.Equal(2000, summary.Income);
        Assert.Equal(1000, summary.Expenses);
        Assert.Equal(1000, summary.Net);
    }

    [Fact]
    public void Spending_by_category_compares_against_budgets()
    {
        var service = new BudgetService();
        service.SetBudget("Groceries", 300);
        service.SetBudget("Entertainment", 50);
        service.Add(Expense("Superstore", 200, "Groceries"));
        service.Add(Expense("No Frills", 150, "Groceries"));
        service.Add(Expense("Bus", 100, "Transportation"));

        var spending = service.SpendingByCategory(2026, 3);
        var groceries = spending.Single(s => s.Category == "Groceries");

        Assert.Equal(350, groceries.Spent);
        Assert.True(groceries.OverBudget);
        Assert.Equal(-50, groceries.Remaining);
        // Budgeted category with no spending still shows up
        Assert.Equal(0, spending.Single(s => s.Category == "Entertainment").Spent);
        // Category without a budget is never "over budget"
        Assert.False(spending.Single(s => s.Category == "Transportation").OverBudget);
        Assert.Equal("Groceries", spending[0].Category);
    }

    [Fact]
    public void Search_filters_by_category_and_text()
    {
        var service = new BudgetService();
        service.Add(Expense("Tim Hortons", 5, "Eating Out", 3));
        service.Add(Expense("Pizza night", 30, "Eating Out", 9));
        service.Add(Expense("Superstore", 80, "Groceries", 5));

        Assert.Equal(2, service.Search(2026, 3, category: "Eating Out").Count());
        Assert.Equal("Pizza night", service.Search(2026, 3, text: "pizza").Single().Description);
        // Newest first
        Assert.Equal("Pizza night", service.Search(2026, 3).First().Description);
    }

    [Fact]
    public void Update_and_delete_change_the_list()
    {
        var service = new BudgetService();
        var t = Expense("Coffee", 4, "Eating Out");
        service.Add(t);

        service.Update(new Transaction { Id = t.Id, Description = "Coffee", Amount = 6, Category = "Eating Out", Date = t.Date });
        Assert.Equal(6, service.Transactions.Single().Amount);

        Assert.True(service.Delete(t.Id));
        Assert.Empty(service.Transactions);
        Assert.False(service.Delete(t.Id));
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Lunch", 0)]
    [InlineData("Lunch", -5)]
    public void Invalid_transactions_are_rejected(string description, decimal amount)
    {
        var service = new BudgetService();
        Assert.Throws<ArgumentException>(() => service.Add(Expense(description, amount, "Other")));
    }

    [Fact]
    public void History_returns_the_last_n_months_oldest_first()
    {
        var service = new BudgetService();
        service.Add(Income(1000, month: 1));
        service.Add(Expense("Rent", 400, "Rent", month: 3));

        var history = service.History(2026, 3, count: 3);

        Assert.Equal([1, 2, 3], history.Select(h => h.Month));
        Assert.Equal(1000, history[0].Income);
        Assert.Equal(400, history[2].Expenses);
    }

    [Fact]
    public void Export_csv_quotes_descriptions_with_commas()
    {
        var service = new BudgetService();
        service.Add(Expense("Shoes, socks", 49.5m, "Shopping", 2));

        var csv = service.ExportCsv(2026, 3);

        Assert.StartsWith("Date,Description,Category,Type,Amount", csv);
        Assert.Contains("2026-03-02,\"Shoes, socks\",Shopping,Expense,49.50", csv);
    }

    [Fact]
    public void Changed_event_fires_on_every_edit()
    {
        var service = new BudgetService();
        var count = 0;
        service.Changed += () => count++;
        var t = Expense("Bus", 3, "Transportation");
        service.Add(t);
        service.SetBudget("Transportation", 100);
        service.Delete(t.Id);
        Assert.Equal(3, count);
    }

    [Fact]
    public void Sample_data_never_contains_future_transactions()
    {
        var today = new DateOnly(2026, 3, 10);
        var data = SampleData.Transactions(today);
        Assert.NotEmpty(data);
        Assert.All(data, t => Assert.True(t.Date <= today));
    }
}
