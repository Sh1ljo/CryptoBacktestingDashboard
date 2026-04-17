using CryptoBacktestingDashboard.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<BacktestSessionMockRepository>();
builder.Services.AddSingleton<BacktestStrategyMockRepository>();
builder.Services.AddSingleton<CryptoPairMockRepository>();
builder.Services.AddSingleton<IndicatorMockRepository>();
builder.Services.AddSingleton<RiskManagementMockRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
