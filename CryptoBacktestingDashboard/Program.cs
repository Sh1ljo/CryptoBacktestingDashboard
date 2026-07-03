using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Repositories;
using CryptoBacktestingDashboard.Repositories.EF;
using CryptoBacktestingDashboard.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

using CryptoBacktestingDashboard.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Serilog: read configuration from appsettings.json
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<AppUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        var clientId = builder.Configuration["Authentication:Google:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.ClientId = string.IsNullOrEmpty(clientId) ? "dummy-client-id" : clientId;
        options.ClientSecret = string.IsNullOrEmpty(clientSecret) ? "dummy-client-secret" : clientSecret;
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<BacktestSessionRepository>();
builder.Services.AddScoped<BacktestStrategyRepository>();
builder.Services.AddScoped<CryptoPairRepository>();
builder.Services.AddScoped<IndicatorRepository>();
builder.Services.AddScoped<IndicatorComparisonRepository>();
builder.Services.AddScoped<CandleDataRepository>();
builder.Services.AddScoped<BacktestResultRepository>();
builder.Services.AddScoped<OptimizationRunRepository>();
builder.Services.AddScoped<OptimizationResultRepository>();

builder.Services.AddHttpClient<MarketDataService>();
builder.Services.AddHttpClient<DeepSeekService>();
builder.Services.AddScoped<BacktestService>();
builder.Services.AddScoped<OptimizationService>();
builder.Services.AddScoped<AgentTools>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Backfill: accounts created before role seeding existed have no role,
        // which makes [Authorize(Roles = "Admin,User")] reject them.
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        foreach (var user in userManager.Users.ToList())
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                await userManager.AddToRoleAsync(user, "User");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database or seeding roles.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();

public partial class Program { }
