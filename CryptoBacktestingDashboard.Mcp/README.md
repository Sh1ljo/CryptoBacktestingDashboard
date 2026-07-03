# CryptoBacktestingDashboard MCP Server

An [MCP](https://modelcontextprotocol.io) (Model Context Protocol) server that exposes the
Crypto Backtesting Dashboard to agentic IDEs (Cursor, Claude Desktop, VS Code, etc.).

It reuses the application's existing `AgentTools` registry — the same tools that power the
in-app AI chat — so there is a single source of truth for all tool definitions and handlers.

## What it exposes

16 tools over **stdio**:

| Area        | Tools |
|-------------|-------|
| Crypto pairs| `list_pairs`, `get_pair`, `add_pair`, `delete_pair`, `fetch_candles` |
| Strategies  | `list_strategies`, `get_strategy`, `create_strategy`, `delete_strategy` |
| Sessions    | `list_sessions`, `get_session`, `create_session`, `run_backtest`, `delete_session` |
| Indicators  | `list_indicators`, `create_indicator` |

`fetch_candles` pulls OHLCV history from Binance; `run_backtest` executes the backtest engine.

## How it works

- **Transport:** stdio. The IDE launches the server as a child process and talks JSON-RPC
  over stdin/stdout. All logging is redirected to **stderr** so it never corrupts the
  protocol stream.
- **Data:** connects to the same SQL Server LocalDB as the web app
  (`appsettings.json` → `ConnectionStrings:DefaultConnection`).
- **User identity:** the app scopes data per user. Since there is no browser login here, the
  server acts as the user whose email is set in `appsettings.json` → `Mcp:UserEmail`
  (default `gabyshiljo@gmail.com`), falling back to the first user in the database.
- **Content root:** anchored to the binary's own directory, so the IDE can launch it from any
  working directory and still find `appsettings.json`.

## Build

```powershell
dotnet build CryptoBacktestingDashboard.Mcp/CryptoBacktestingDashboard.Mcp.csproj
```

This produces the launch target:
`CryptoBacktestingDashboard.Mcp/bin/Debug/net8.0/CryptoBacktestingDashboard.Mcp.dll`

> Use the built **DLL** in IDE config — not `dotnet run`, which writes build output to
> stdout and corrupts the stdio protocol.

## Connect from Cursor

A ready-to-use config is checked in at `.cursor/mcp.json` (project root). Open the project in
Cursor, go to **Settings → MCP**, and the `crypto-backtesting-dashboard` server should appear.
Click refresh/enable if needed.

## Connect from Claude Desktop

Edit `claude_desktop_config.json`
(`%APPDATA%\Claude\claude_desktop_config.json` on Windows) and add:

```json
{
  "mcpServers": {
    "crypto-backtesting-dashboard": {
      "command": "dotnet",
      "args": [
        "C:\\Projects\\CryptoBacktestingDashboard\\CryptoBacktestingDashboard.Mcp\\bin\\Debug\\net8.0\\CryptoBacktestingDashboard.Mcp.dll"
      ]
    }
  }
}
```

Restart Claude Desktop. The tools appear under the 🔌 (plug) icon.

## Manual smoke test

```bash
DLL="CryptoBacktestingDashboard.Mcp/bin/Debug/net8.0/CryptoBacktestingDashboard.Mcp.dll"
{ printf '%s\n' \
  '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}' \
  '{"jsonrpc":"2.0","method":"notifications/initialized"}' \
  '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' ; sleep 4; } | dotnet "$DLL"
```

You should see the `initialize` result followed by the list of 16 tools.
