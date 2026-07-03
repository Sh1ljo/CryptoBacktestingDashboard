using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Mcp;
using CryptoBacktestingDashboard.Repositories.EF;
using CryptoBacktestingDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using System.Text.Json;

// Anchor the content root (and therefore appsettings.json discovery) to the binary's
// own directory. An agentic IDE launches this server with an arbitrary working
// directory, so relying on the CWD to find appsettings.json is unreliable.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// IMPORTANT: stdout is the JSON-RPC channel for stdio transport.
// All logs must go to stderr or they corrupt the protocol stream.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// ── Application services (mirrors the web app's Program.cs, minus web/identity UI) ──
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<BacktestSessionRepository>();
builder.Services.AddScoped<BacktestStrategyRepository>();
builder.Services.AddScoped<CryptoPairRepository>();
builder.Services.AddScoped<IndicatorRepository>();
builder.Services.AddScoped<IndicatorComparisonRepository>();
builder.Services.AddScoped<CandleDataRepository>();
builder.Services.AddScoped<BacktestResultRepository>();

builder.Services.AddHttpClient<MarketDataService>();
builder.Services.AddScoped<BacktestService>();
builder.Services.AddScoped<AgentTools>();

// Resolves the user the MCP server acts as (per-user data scoping).
builder.Services.AddSingleton<McpUserResolver>();

// ── MCP server: bridge the existing AgentTools registry to MCP tools ──
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "crypto-backtesting-dashboard",
            Version = "1.0.0"
        };
        options.ServerInstructions =
            "Tools for managing a crypto backtesting dashboard: crypto pairs, technical " +
            "indicators, trading strategies, and backtest sessions. You can list/create/delete " +
            "these entities, fetch historical candle data from Binance, and run backtests.";
    })
    .WithStdioServerTransport()
    .WithListToolsHandler((ctx, ct) =>
    {
        using var scope = ctx.Services!.CreateScope();
        var agentTools = scope.ServiceProvider.GetRequiredService<AgentTools>();

        var tools = agentTools.GetToolDefinitions()
            .Select(td => new Tool
            {
                Name = td.Name,
                Description = td.Description,
                InputSchema = td.InputSchema
            })
            .ToList();

        return ValueTask.FromResult(new ListToolsResult { Tools = tools });
    })
    .WithCallToolHandler(async (ctx, ct) =>
    {
        var name = ctx.Params?.Name ?? string.Empty;

        // Re-pack the MCP arguments dictionary into a single JsonElement,
        // which is what AgentTools.ExecuteToolAsync consumes.
        var argsDict = ctx.Params?.Arguments ?? new Dictionary<string, JsonElement>();
        var argsElement = JsonSerializer.SerializeToElement(argsDict);

        using var scope = ctx.Services!.CreateScope();
        var sp = scope.ServiceProvider;
        var agentTools = sp.GetRequiredService<AgentTools>();
        var resolver = sp.GetRequiredService<McpUserResolver>();

        var userId = await resolver.GetUserIdAsync(sp, ct);
        if (userId is null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = { new TextContentBlock { Text = "No application user found. Register a user in the web app first." } }
            };
        }

        var (result, success) = await agentTools.ExecuteToolAsync(name, argsElement, userId);

        return new CallToolResult
        {
            IsError = !success,
            Content = { new TextContentBlock { Text = result } }
        };
    });

await builder.Build().RunAsync();
