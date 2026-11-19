using System.Text.Json;
using BudgetTracker.Core;
using Microsoft.JSInterop;

namespace BudgetTracker.Web.Services;

/// <summary>Saves transactions and budgets to the browser's localStorage.</summary>
public class LocalStore(IJSRuntime js)
{
    private const string Key = "budget-tracker-v1";
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    private record Snapshot(List<Transaction> Transactions, List<CategoryBudget> Budgets);

    public async Task<bool> LoadInto(BudgetService service)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (string.IsNullOrEmpty(json)) return false;
            var snapshot = JsonSerializer.Deserialize<Snapshot>(json, Options);
            if (snapshot is null) return false;
            service.Load(snapshot.Transactions, snapshot.Budgets);
            return true;
        }
        catch (Exception)
        {
            // Storage unavailable (private mode) or corrupted: start fresh
            return false;
        }
    }

    public async Task Save(BudgetService service)
    {
        var snapshot = new Snapshot(
            service.Transactions.ToList(),
            service.Budgets.Select(b => new CategoryBudget(b.Key, b.Value)).ToList());
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(snapshot, Options));
        }
        catch (Exception)
        {
            // Ignore: the app still works for this session
        }
    }

    public async Task Clear()
    {
        try { await js.InvokeVoidAsync("localStorage.removeItem", Key); } catch (Exception) { }
    }
}
