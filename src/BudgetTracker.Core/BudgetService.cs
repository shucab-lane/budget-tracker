using System.Globalization;
using System.Text;

namespace BudgetTracker.Core;

/// <summary>Holds transactions and budgets and answers questions about them.</summary>
public class BudgetService
{
    private readonly List<Transaction> _transactions = [];
    private readonly Dictionary<string, decimal> _budgets = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Transaction> Transactions => _transactions;
    public IReadOnlyDictionary<string, decimal> Budgets => _budgets;

    public event Action? Changed;

    public void Load(IEnumerable<Transaction> transactions, IEnumerable<CategoryBudget> budgets)
    {
        _transactions.Clear();
        _transactions.AddRange(transactions);
        _budgets.Clear();
        foreach (var b in budgets) _budgets[b.Category] = b.MonthlyLimit;
        Changed?.Invoke();
    }

    public void Add(Transaction t)
    {
        Validate(t);
        _transactions.Add(t);
        Changed?.Invoke();
    }

    public void Update(Transaction t)
    {
        Validate(t);
        var index = _transactions.FindIndex(x => x.Id == t.Id);
        if (index < 0) throw new KeyNotFoundException($"Transaction {t.Id} not found");
        _transactions[index] = t;
        Changed?.Invoke();
    }

    public bool Delete(Guid id)
    {
        var removed = _transactions.RemoveAll(t => t.Id == id) > 0;
        if (removed) Changed?.Invoke();
        return removed;
    }

    public void SetBudget(string category, decimal monthlyLimit)
    {
        if (monthlyLimit < 0) throw new ArgumentOutOfRangeException(nameof(monthlyLimit), "Budget can't be negative");
        if (monthlyLimit == 0) _budgets.Remove(category);
        else _budgets[category] = monthlyLimit;
        Changed?.Invoke();
    }

    public IEnumerable<Transaction> InMonth(int year, int month) =>
        _transactions.Where(t => t.Date.Year == year && t.Date.Month == month);

    public IEnumerable<Transaction> Search(int year, int month, string? category = null, string? text = null) =>
        InMonth(year, month)
            .Where(t => string.IsNullOrEmpty(category) || t.Category == category)
            .Where(t => string.IsNullOrWhiteSpace(text) || t.Description.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Date)
            .ThenBy(t => t.Description);

    public MonthSummary Summary(int year, int month)
    {
        var items = InMonth(year, month).ToList();
        return new MonthSummary(
            year, month,
            items.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            items.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount));
    }

    /// <summary>Spending per category for a month, including budgeted categories with no spending yet.</summary>
    public List<CategorySpending> SpendingByCategory(int year, int month)
    {
        var spent = InMonth(year, month)
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        return spent.Keys.Union(_budgets.Keys)
            .Select(c => new CategorySpending(c, spent.GetValueOrDefault(c), _budgets.TryGetValue(c, out var l) ? l : null))
            .OrderByDescending(c => c.Spent)
            .ThenBy(c => c.Category)
            .ToList();
    }

    /// <summary>Income and expenses for the last <paramref name="count"/> months, oldest first.</summary>
    public List<MonthSummary> History(int year, int month, int count = 6)
    {
        var start = new DateOnly(year, month, 1).AddMonths(-(count - 1));
        return Enumerable.Range(0, count)
            .Select(i => start.AddMonths(i))
            .Select(d => Summary(d.Year, d.Month))
            .ToList();
    }

    public string ExportCsv(int year, int month)
    {
        var sb = new StringBuilder("Date,Description,Category,Type,Amount\n");
        foreach (var t in InMonth(year, month).OrderBy(t => t.Date))
        {
            var description = t.Description.Contains(',') || t.Description.Contains('"')
                ? $"\"{t.Description.Replace("\"", "\"\"")}\""
                : t.Description;
            sb.Append(CultureInfo.InvariantCulture, $"{t.Date:yyyy-MM-dd},{description},{t.Category},{t.Type},{t.Amount:0.00}\n");
        }
        return sb.ToString();
    }

    private static void Validate(Transaction t)
    {
        if (string.IsNullOrWhiteSpace(t.Description)) throw new ArgumentException("Description is required");
        if (t.Amount <= 0) throw new ArgumentException("Amount must be greater than zero");
    }
}
