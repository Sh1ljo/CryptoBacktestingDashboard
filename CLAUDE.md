# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```powershell
# Run the application
dotnet run --project CryptoBacktestingDashboard/CryptoBacktestingDashboard.csproj

# Build
dotnet build

# Add a new EF migration
dotnet ef migrations add <MigrationName> --project CryptoBacktestingDashboard

# Apply migrations manually (also happens automatically on startup)
dotnet ef database update --project CryptoBacktestingDashboard
```

## Architecture

ASP.NET Core MVC app (.NET 8) backed by SQL Server LocalDB. The database auto-migrates on startup via `context.Database.Migrate()` in `Program.cs`.

**Connection string**: `appsettings.json` → `ConnectionStrings:DefaultConnection` (LocalDB, database `CryptoBacktestingDashboard`).

### Layers

- **Models** (`Models/Crypto/`): Domain entities. `BacktestSession` has `GetProfit()`/`GetROI()` and a `HasRun` flag (`FinalBalance > 0`); both return **0 until the session has been run** (a fresh session has `FinalBalance == 0`, so don't show its profit as `-InitialBalance`). `BacktestResult` has `GetProfit()` and `GetProfitPercent()` — the percent is computed off realized dollars **net of commission**, so it agrees with the dollar P/L.
- **Services** (`Services/`): Business logic layer. Currently:
  - **`MarketDataService`** — fetches historical OHLCV candle data from the Binance public API (no auth needed). Converts symbols (e.g., `BTC/USD` → `BTCUSDT` by stripping the quote suffix and appending `USDT`), deduplicates by timestamp, bulk-inserts `CandleData` rows, and updates `CryptoPair.CurrentPrice` to the latest close. **Must use `CultureInfo.InvariantCulture`** when parsing Binance's decimal strings (e.g., `"81024.90"`) since the server locale (`hr-HR`) uses `.` as the thousands separator. Registered via `AddHttpClient<MarketDataService>()` in `Program.cs`.
  - **`IndicatorCalculator`** (`Services/IndicatorCalculator.cs`) — pure static math class. One method per `IndicatorType` enum (RSI, MACD, SMA, EMA, BollingerBands, Stochastic, ATR). All return `List<decimal?>` where null values are returned for warmup periods with insufficient data. The nullable-list EMA overload (used for the MACD signal line) seeds off the first run of consecutive non-null values, so a leading null prefix doesn't throw.
  - **`StrategyEvaluator`** (`Services/StrategyEvaluator.cs`) — static class with a `TradingSignal` enum (`Buy`/`Sell`/`Hold`). `Evaluate()` translates indicator values into signals based on thresholds and crossovers. RSI/Stochastic > threshold = Sell, < (100-threshold) = Buy (both honor the indicator's configured `Threshold`). MACD histogram crossing zero. Price/MA crossovers. Bollinger band touches. **The single `Indicator.Threshold` column means different things per type** — RSI/Stochastic: overbought level; MACD: slow-EMA period; Bollinger: std-dev multiplier; MovingAverage/ATR: unused. The Indicator Create/Edit form (`wwwroot/js/indicator-form.js`) relabels the field per type, and `IndicatorController` validates ranges per type.
  - **`BacktestService`** (`Services/BacktestService.cs`) — core engine injected as scoped. `RunBacktestAsync(session)` loads candle data, iterates candle-by-candle computing indicators progressively, evaluates entry/exit signals, sizes the position, applies the strategy's built-in Stop Loss + Take Profit (+ optional Trailing Stop) every candle, and produces `BacktestResult` trades. Minimum data requirement is based on the longest indicator period, not the strategy's LookbackPeriod; the first traded candle is `warmup + 1` so both current and previous indicator values are valid. **Signal quirk:** both attached indicators *and* comparison rules feed the combined signal, so attaching a `MovingAverage` indicator that's also used in a crossover comparison produces a standalone price-vs-MA signal on top of the crossover.
- **Repositories** (`Repositories/EF/`): One async repository per entity, injected as scoped services. All follow the same `GetItemsAsync / GetItemAsync / InsertItemAsync / UpdateItemAsync / DeleteItemAsync` interface. Mock repositories exist under `Repositories/` but are unused — only the EF variants are registered.
- **Controllers**: Route-attribute-based, not convention-based. Each entity has its own controller with a `[Route("...")]` prefix.
- **Views** (`Views/{ControllerName}/`): Razor views with Bootstrap styling. Use ASP.NET Tag Helpers (`asp-for`, `asp-validation-for`, etc.).

### Domain model relationships

```
BacktestStrategy  ──N:N──▶  Indicator
        │
       1:N
        ▼
CryptoPair  ──────1:N──────  BacktestSession  ──1:N──▶  BacktestResult
    │
   1:N
    ▼
CandleData
```

**Risk management is built into `BacktestStrategy`** (no separate entity). Each strategy
carries required `StopLossPercent` + `TakeProfitPercent`, and optional `TrailingStopPercent`
and `PositionSizePercent`. `RiskRewardRatio` is a computed `[NotMapped]` helper (TP ÷ SL).
The old standalone `RiskManagement` entity/controller/menu was retired (see migration
`EmbedRiskInStrategy`). The engine applies Stop Loss + Take Profit together each candle, with
the trailing stop tightening the effective stop as price rises.

### Shared partials

Two reusable partials under `Views/Shared/`:

- **`_AutocompleteDropdownPartial.cshtml`** — requires `AutocompleteViewModel` model. Renders a text search input + hidden field pair, calls a JSON endpoint (e.g. `GET /strategies/search?query=...`) and populates a dropdown. Each searchable controller exposes a `Search(string query)` action returning `[{ id, text }]`.
- **`_DatepickerPartial.cshtml`** — takes a `DateTime?` model, dynamically loads Flatpickr from CDN if missing. Stores ISO value in a hidden input for form binding. Adapts date format based on Croatian (`hr`) vs. other culture.

### AJAX pattern

Index actions and Delete actions detect `X-Requested-With: XMLHttpRequest` and return partial views / `Ok()` instead of full pages. This allows list pages to refresh without a full reload.

### ModelState navigation-property scrubbing

When binding forms, navigation properties are not posted and will fail validation. Controllers explicitly call `ModelState.Remove("PropertyName")` for each before checking `ModelState.IsValid`. Do the same in any new Create/Edit actions.

**Navigation properties to remove per entity:**

- `BacktestSession`: `Strategy`, `CryptoPair`, `Results`
- `BacktestStrategy`: `Indicators`, `BacktestSessions`, `Comparisons`
- `CryptoPair`: `CandleDataHistory`, `BacktestSessions`
- `Indicator`: `Strategies`

### View patterns

See `CryptoBacktestingDashboard/Views/SKILL.md` and `CryptoBacktestingDashboard/.github/skills/edit-form.md` for detailed guidance on creating list pages and edit/create forms.

### CRUD Status

All entities have complete Create, Read, Update, Delete functionality:

- **BacktestSession** - Full CRUD with search on Strategy/Pair symbol ✓
- **BacktestStrategy** - Full CRUD with built-in risk fields (SL/TP/trailing/sizing) and Indicator checklist ✓
- **CryptoPair** - Full CRUD with search on Symbol ✓
- **Indicator** - Full CRUD with search on Name/Description ✓

Each entity follows the same pattern:

1. **Index** - List with AJAX search, + New button, edit/delete action buttons
2. **Create** - Form with validation and proper ModelState cleanup
3. **Edit** - Form with pre-populated data and timestamp preservation
4. **Delete** - AJAX-enabled with confirmation dialog

### Data Fetching

`POST /pairs/{id}/fetch-data` (on `CryptoPairController`) — calls `MarketDataService.FetchCandlesAsync()` to pull daily OHLCV data from Binance's public `/api/v3/klines` endpoint for the given pair. Deduplicates by `OpenTime`, bulk-inserts new `CandleData` rows, and updates the pair's `CurrentPrice` to the latest close. Idempotent — safe to call repeatedly.

`POST /pairs/{id}/clear-data` (on `CryptoPairController`) — deletes all `CandleData` rows for the given pair and resets `CurrentPrice` to 0. Useful for purging bad data (e.g., after a locale parsing bug or wrong symbol mapping).

`CryptoPair.CurrentPrice` is **nullable** (`decimal?`). New pairs have no price until data is fetched, at which point `MarketDataService` sets it to the latest candle's close. Views display `$0.00` when null.

### Backtesting Engine

`POST /backtests/{id}/run` (on `BacktestSessionController`) — loads the session with its strategy and indicators, fetches candle data via `CandleDataRepository.GetByPairIdAndDateRangeAsync()`, then runs `BacktestService.RunBacktestAsync()`. The engine:
1. Skips warmup candles based on the **longest indicator period** (not the strategy's `LookbackPeriod`)
2. At each candle, computes all strategy indicators progressively
3. Evaluates combined signals (any Buy → Buy, any Sell → Sell, conflicting → Hold)
4. Opens Long positions on Buy signals, closes on Sell signals or risk management triggers
5. Applies **Stop Loss + Take Profit together** every candle (both required on the strategy), plus an optional **Trailing Stop** that tightens the effective stop as price rises. If a single candle spans both stop and target, the stop is assumed hit first (conservative).
6. Position sizing: deploys `PositionSizePercent` of available cash per trade (default 100%)
7. Commission: **0.1% of trade notional per side** (`CommissionRate` in `BacktestService`), charged on open and close and stored on each `BacktestResult`

Results are persisted as `BacktestResult` rows and the session's `FinalBalance`/`ExecutedAt` are updated. Old results are deleted before each run — reruns replace them entirely.

### Sessions & results UI

- **Session Details** (`Views/BacktestSession/Details.cshtml`) shows key metrics, an **equity curve** (Chart.js loaded from CDN on this page only, reconstructed from the trades — each `BacktestResult.GetProfit()` is its cash delta, summed from `InitialBalance`), and a trade table. The Run button shows a "Running…" spinner on submit. Dollar P/L is sign-prefixed (`-$…`) so losses read correctly.
- **Session Create** (`Views/BacktestSession/Create.cshtml`) calls `GET /pairs/{id}/data-status` when a pair is picked and warns if the pair has no candle data (avoids the cryptic "not enough candle data" failure).
- **Sessions Index** is sorted by `GetProfit()` descending (most profitable first; un-run sessions sit at 0).
- **Strategy Create/Edit** use a checkbox list of indicators (not a Ctrl-click multiselect), built-in risk inputs with a live Risk:Reward readout, and quick-start templates that auto-tick matching indicators and fill risk values. Strategy **Details** lists the comparison rules in plain English and the built-in risk as cards.

### Migrations note

`IndicatorComparison` has two FKs to `Indicator` (`IndicatorAId`, `IndicatorBId`). Both are explicitly configured `OnDelete(DeleteBehavior.Restrict)` in `OnModelCreating` — cascade on both would create multiple cascade paths to `Indicators` (SQL error 1785). Keep them Restrict when scaffolding future migrations. The `EmbedRiskInStrategy` migration dropped the old `RiskManagements` table and added the strategy risk columns (existing rows backfilled SL 5% / TP 10%).
