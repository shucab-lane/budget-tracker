namespace BudgetTracker.Core;

/// <summary>Realistic starter data so the app isn't empty on first visit.</summary>
public static class SampleData
{
    public static List<Transaction> Transactions(DateOnly today)
    {
        var list = new List<Transaction>();
        var random = new Random(42);
        for (var m = 5; m >= 0; m--)
        {
            var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-m);
            var lastDay = m == 0 ? today.Day : DateTime.DaysInMonth(first.Year, first.Month);
            DateOnly Day(int d) => first.AddDays(Math.Min(d, lastDay) - 1);

            list.Add(new() { Description = "Part-time job", Amount = 1450m, Type = TransactionType.Income, Category = "Salary", Date = Day(1) });
            list.Add(new() { Description = "Part-time job", Amount = 1450m, Type = TransactionType.Income, Category = "Salary", Date = Day(15) });
            if (m % 2 == 0)
                list.Add(new() { Description = "Website for a client", Amount = 300m + random.Next(0, 4) * 50, Type = TransactionType.Income, Category = "Freelance", Date = Day(20) });

            list.Add(new() { Description = "Rent", Amount = 950m, Category = "Rent", Date = Day(1) });
            list.Add(new() { Description = "ETS bus pass", Amount = 100m, Category = "Transportation", Date = Day(2) });
            list.Add(new() { Description = "Phone plan", Amount = 55m, Category = "Phone & Internet", Date = Day(5) });
            list.Add(new() { Description = "EPCOR utilities", Amount = 80m + random.Next(0, 40), Category = "Utilities", Date = Day(12) });
            list.Add(new() { Description = "Spotify", Amount = 11.99m, Category = "Entertainment", Date = Day(8) });

            string[] stores = ["Superstore", "Save-On-Foods", "No Frills", "T&T Market"];
            for (var w = 0; w < 4; w++)
                list.Add(new() { Description = stores[random.Next(stores.Length)], Amount = 55m + random.Next(0, 60), Category = "Groceries", Date = Day(3 + w * 7) });

            string[] food = ["Tim Hortons", "Pizza night", "Shawarma", "Coffee with friends", "Pho"];
            for (var i = 0; i < 3 + random.Next(0, 3); i++)
                list.Add(new() { Description = food[random.Next(food.Length)], Amount = 8m + random.Next(0, 30), Category = "Eating Out", Date = Day(4 + i * 6) });

            if (random.NextDouble() < 0.6)
                list.Add(new() { Description = "West Edmonton Mall", Amount = 40m + random.Next(0, 90), Category = "Shopping", Date = Day(18) });
        }
        return list.Where(t => t.Date <= today).ToList();
    }

    public static List<CategoryBudget> Budgets() =>
    [
        new("Rent", 950m),
        new("Groceries", 350m),
        new("Eating Out", 80m),
        new("Transportation", 120m),
        new("Entertainment", 40m),
        new("Shopping", 100m),
    ];
}
