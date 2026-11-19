using BudgetTracker.Core;
using BudgetTracker.Web;
using BudgetTracker.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<BudgetService>();
builder.Services.AddSingleton<LocalStore>();

await builder.Build().RunAsync();
