# Optimization Engine — Implementation Plan

## Overview

Add an **Optimization** feature to the Session backtest flow. When the user presses "Optimize" on a Session Details page, the system runs many backtests in parallel with different parameter combinations, collects all results, ranks them by a composite score, displays the winning configuration, and lets the user apply it with one click.

---

## 1. Domain & Model Changes

### 1.1 New Model: `OptimizationRun`

```csharp
public class OptimizationRun
{
    public int Id { get; set; }
    public int BacktestSessionId { get; set; }

    // The grid of parameters tested (serialized JSON)
    public string ParameterGridJson { get; set; }

    // The winning combination (serialized JSON)
    public string BestParamsJson { get; set; }

    // Composite score of the winner
    public double BestCompositeScore { get; set; }

    // Total combinations tested
    public int TotalCombinations { get; set; }

    // When the optimization ran
    public DateTime RanAt { get; set; }

    // FK
    public virtual BacktestSession BacktestSession { get; set; }
}
```

**Migration needed**: `AddOptimizationRun` table with FK to `BacktestSession`.

### 1.2 New Model: `OptimizationResult`

Stores individual combo results so the UI can show a comparison table if desired.

```csharp
public class OptimizationResult
{
    public int Id { get; set; }
    public int OptimizationRunId { get; set; }

    // The parameter values used (serialized JSON for flexibility)
    public string ParamsJson { get; set; }

    // Composite score (0–100)
    public double CompositeScore { get; set; }

    // Raw metrics for display
    public decimal Profit { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal WinRate { get; set; }
    public decimal MaxDrawdownPercent { get; set; }
    public int TotalTrades { get; set; }
    public decimal SharpeApprox { get; set; }

    public virtual OptimizationRun OptimizationRun { get; set; }
}
```

**Migration needed**: `AddOptimizationResult` table.

### 1.3 `BacktestSession` — Minor Addition

The existing `IsOptimized` bool on `BacktestSession` is already there. No field changes needed on sessions.

### 1.4 `OptimizerProfile` — Parameter Range Definition

Define what ranges each variable will sweep. This is the *input* to optimization — not persisted, just a UI model.

```csharp
public class OptimizationProfile
{
    // Which indicator parameters to sweep
    public Dictionary<int, ParameterSweep> IndicatorSweeps { get; set; }
    // Keyed by Indicator.Id, each with Period range and Threshold range

    // Risk parameter sweeps
    public ParameterSweep? StopLossSweep { get; set; }
    public ParameterSweep? TakeProfitSweep { get; set; }
    public ParameterSweep? TrailingStopSweep { get; set; }
    public ParameterSweep? PositionSizeSweep { get; set; }
}

public class ParameterSweep
{
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Step { get; set; }
}
```

---

## 2. Scoring Engine

### 2.1 `OptimizationService` — New Service

A new service that:
1. Expands the user's parameter ranges into a flat list of all combinations.
2. For each combination, clones the strategy settings, runs `BacktestService.RunBacktestAsync()` (reusing the same candle data).
3. Computes a **composite score** for each result.
4. Sorts and returns the best.

### 2.2 Composite Score Formula

Score (0–100) based on multiple weighted metrics:

| Metric              | Weight | Notes                                           |
|---------------------|--------|-------------------------------------------------|
| Profit              | 25%    | Net P/L, normalized against max observed        |
| Profit Factor       | 20%    | Gross Profit / Gross Loss, capped at 10         |
| Win Rate            | 15%    | Winning trades / total trades, 0–100%           |
| Max Drawdown        | 20%    | Penalize large drawdowns (inverted)             |
| Sharpe (approx)     | 10%    | Avg trade return / std dev of trade returns     |
| Trade Count         | 10%    | Penalize < 5 trades (insufficient data)         |

Formula outline:

```
score = 0
  + (profitNormalized * 0.25)
  + (min(profitFactor, 10) / 10 * 0.20)
  + (winRate / 100 * 0.15)
  + ((1 - abs(maxDrawdownPct) / 100) * 0.20)
  + (min(sharpe, 3) / 3 * 0.10)
  + (tradeCount >= 5 ? 0.10 : (tradeCount / 5) * 0.10)
score *= 100
```

This ensures a single, transparent, multi-dimensional ranking.

### 2.3 `OptimizationService` — Key Methods

```csharp
public class OptimizationService
{
    // Generates all parameter combinations from the profile
    public List<Dictionary<string, decimal>> ExpandParameterGrid(OptimizationProfile profile, BacktestStrategy strategy);

    // Runs the full optimization (can be called asynchronously)
    public Task<OptimizationRun> RunOptimizationAsync(
        BacktestSession session,
        OptimizationProfile profile);

    // Applies winning params back to the strategy
    public Task ApplyBestParamsAsync(BacktestStrategy strategy, Dictionary<string, decimal> bestParams);
}
```

---

## 3. Backend / Controller Changes

### 3.1 New API Controller: `OptimizationApiController`

REST endpoints to drive optimization from the frontend.

| Method | Route                                | Action                                |
|--------|--------------------------------------|---------------------------------------|
| POST   | `/api/optimization/{sessionId}/start` | Start optimization, return run ID     |
| GET    | `/api/optimization/{runId}/status`    | Poll: % complete, current combo       |
| GET    | `/api/optimization/{runId}/result`    | Full result with best params + metrics|
| POST   | `/api/optimization/{runId}/apply`     | Apply best params to the strategy     |

### 3.2 `BacktestSessionController` — Add `Optimize` GET action

```csharp
[HttpGet("{id}/optimize")]
public async Task<IActionResult> Optimize(int id)
{
    var session = await _sessionRepository.GetItemAsync(id);
    if (session == null) return NotFound();
    // Load strategy with full indicators for building the profile UI
    var strategy = await _strategyRepository.GetItemAsync(session.StrategyId);
    session.Strategy = strategy;
    return View(session);
}
```

This renders the **Optimize page** — a form where the user defines sweep ranges.

### 3.3 DTO for the Profile Form

```csharp
public class OptimizationProfileDTO
{
    // For each indicator on the strategy: minPeriod, maxPeriod, stepPeriod, minThreshold, maxThreshold, stepThreshold
    public List<IndicatorSweepDTO> Indicators { get; set; }
    public decimal? SlMin, SlMax, SlStep;
    public decimal? TpMin, TpMax, TpStep;
    public decimal? TsMin, TsMax, TsStep;
    public decimal? PsMin, PsMax, PsStep;
}
```

---

## 4. UI / View Changes

### 4.1 New View: `Views/BacktestSession/Optimize.cshtml`

A configuration page where the user sets parameter ranges for optimization. Layout:

```
Breadcrumbs: Sessions / Strategy Pair / Optimize

┌─────────────────────────────────────────────────────┐
│  Optimization Configuration                         │
│                                                     │
│  ┌─ Indicator Parameters ────────────────────────┐  │
│  │  For each indicator on the strategy:           │  │
│  │  ┌── RSI (14) ─────────────────────────────┐  │  │
│  │  │  Period:  [ 7 ] to [ 21 ] step [ 2 ]    │  │  │
│  │  │  Threshold: [ 60 ] to [ 80 ] step [ 5 ] │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  │  ┌── MovingAverage (20) ────────────────────┐  │  │
│  │  │  Period:  [ 10 ] to [ 50 ] step [ 5 ]   │  │  │
│  │  └─────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─ Risk Parameters ────────────────────────────┐  │
│  │  Stop Loss %:    [ 2 ] to [ 10 ] step [ 1 ] │  │
│  │  Take Profit %:  [ 5 ] to [ 20 ] step [ 2 ] │  │
│  │  Trailing Stop:  [ 0 ] to [ 5 ] step [ 0.5 ]│  │
│  │  Position Size:  [ 50 ] to [ 100 ] step [10]│  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  [ ◉ Start Optimization ]  Estimated: ~240 combos  │
└─────────────────────────────────────────────────────┘
```

**Design**: Uses the same Revolut-inspired design system (`.card`, `.form-card`, `.form-group`, pill buttons, typography hierarchy). Each indicator card uses the same pattern as the strategy details page.

### 4.2 New View: `Views/BacktestSession/OptimizationResult.cshtml` (or Partial)

Shown **inline on the Optimize page** after the run completes (via AJAX). Shows:

```
┌─────────────────────────────────────────────────────┐
│  ✓ Optimization Complete — Best Configuration       │
│                                                     │
│  ┌─ Performance Metrics ────────────────────────┐  │
│  │  Score: 87.4 | Profit: +$1,234 | ROI: 12.3% │  │
│  │  PF: 2.1 | Win Rate: 58% | Max DD: -8.2%    │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  ┌─ Best Parameters ────────────────────────────┐  │
│  │  RSI Period: 14  │  Threshold: 70             │  │
│  │  MA Period: 20                                │  │
│  │  Stop Loss: 5%  │  Take Profit: 12%           │  │
│  │  Trailing Stop: 2%  │  Position Size: 100%    │  │
│  └───────────────────────────────────────────────┘  │
│                                                     │
│  [ ✦ Apply to Strategy ]  [ View All Results ]     │
└─────────────────────────────────────────────────────┘
```

### 4.3 Progress Animation

While optimization is running, show a **determinate progress bar** with real-time updates:

- **Progress bar**: Full-width, rounded, animated fill with the Revolut green (`#00a87e`).
- **Status text**: "Testing combination 127 of 240..."
- **Live metrics ticker**: Shows the best score found so far and which combo holds it.
- **Spinning icon**: A subtle pulse/spin on the Optimize button text changes to a gear animation.

CSS for the progress animation will use the existing `@keyframes` pattern in `custom.css`:

```css
@keyframes pulse-progress {
    0% { opacity: 0.6; }
    50% { opacity: 1; }
    100% { opacity: 0.6; }
}

.optimization-progress {
    height: 6px;
    border-radius: 9999px;
    background: var(--rui-border);
    overflow: hidden;
}

.optimization-progress-bar {
    height: 100%;
    background: var(--rui-success);
    border-radius: 9999px;
    transition: width 0.3s ease;
}

.optimization-status {
    animation: pulse-progress 1.5s ease-in-out infinite;
}
```

The progress is driven by polling `GET /api/optimization/{runId}/status` every 500ms via `setInterval`.

### 4.4 JavaScript — `optimization-flow.js`

A new JS file under `wwwroot/js/` that handles:

1. **Start**: Submit the profile form via `fetch()` POST to `/api/optimization/{sessionId}/start`.
2. **Polling**: `setInterval` to GET `/api/optimization/{runId}/status`, updating:
   - Progress bar width
   - Combination counter text
   - Best-score-so-far line
3. **Completion**: Stop polling, render the result card via a fetched partial or JSON.
4. **Apply**: POST `/api/optimization/{runId}/apply` → reload the page or show a toast "Strategy updated!".

---

## 5. Optimization Flow — Step by Step

```
User on Session Details page
  │
  ├── Clicks [ ⚡ Optimize ] button
  │
  ▼
Optimize.cshtml — Configure sweep ranges
  │
  ├── Clicks [ ◉ Start Optimization ]
  │
  ▼
POST /api/optimization/{sessionId}/start
  │
  ├── Server expands parameter grid
  ├── Creates OptimizationRun row (status = "running")
  ├── Returns { runId }
  │
  ▼
Client starts polling GET /api/optimization/{runId}/status
  │
  ├── Server returns { completed, total, bestScoreSoFar, currentCombo }
  ├── Client updates progress bar + status
  ├── ─── loop every 500ms ───
  │
  ▼
Optimization completes on server
  │
  ├── Saves OptimizationRun (status = "completed", BestParamsJson, CompositeScore)
  ├── Saves all OptimizationResult rows
  │
  ▼
Client's poll receives { completed: total, status: "completed" }
  │
  ├── Client stops polling
  ├── Fetches GET /api/optimization/{runId}/result
  ├── Renders result card with metrics + best params
  │
  ▼
User clicks [ ✦ Apply to Strategy ]
  │
  ├── POST /api/optimization/{runId}/apply
  ├── Server updates BacktestStrategy with best params
  ├── Sets session.IsOptimized = true
  ├── Returns success
  │
  ▼
Toast: "Strategy updated with optimized parameters!"
  │
  ├── Optionally: redirect to session details
  └── User sees the new settings applied
```

---

## 6. Files to Create / Modify

### New Files

| File | Purpose |
|------|---------|
| `Models/Crypto/OptimizationRun.cs` | Optimization run entity |
| `Models/Crypto/OptimizationResult.cs` | Individual combo result entity |
| `Models/Crypto/OptimizationProfile.cs` | DTO / model for sweep ranges |
| `Models/DTO/OptimizationProfileDTO.cs` | Form-bound profile input |
| `Models/DTO/OptimizationStatusDTO.cs` | Polling response DTO |
| `Models/DTO/OptimizationResultDTO.cs` | Final result DTO |
| `Services/OptimizationService.cs` | Core optimization engine |
| `Controllers/Api/OptimizationApiController.cs` | AJAX endpoints |
| `Views/BacktestSession/Optimize.cshtml` | Configuration page |
| `Views/BacktestSession/_OptimizationResultPartial.cshtml` | Result display partial |
| `wwwroot/js/optimization-flow.js` | Client-side polling + animation |

### Modified Files

| File | Changes |
|------|---------|
| `Data/ApplicationDbContext.cs` | Add `DbSet<OptimizationRun>`, `DbSet<OptimizationResult>` |
| `Views/BacktestSession/Details.cshtml` | Add "⚡ Optimize" button next to "Run Backtest" |
| `wwwroot/css/custom.css` | Add progress bar + optimization animation styles |
| `Models/Crypto/BacktestSession.cs` | Add `OptimizationRun` navigation property |
| `Controllers/BacktestSessionController.cs` | Add `Optimize` GET action |
| `Program.cs` | Register `OptimizationService`, `OptimizationApiController` routes |

---

## 7. Parameter Grid Expansion Logic

The `OptimizationService.ExpandParameterGrid()` works as follows:

```
Input: OptimizationProfile with ranges for each indicator and risk params

For each Indicator on the strategy:
  - Generate Period values: [min, min+step, min+2*step, ..., max]
  - Generate Threshold values: [min, min+step, min+2*step, ..., max]

For risk params:
  - Generate SL values, TP values, TS values, PS values

Cartesian product of all parameter lists → flat combo list
```

**Example**: A strategy with 2 indicators (RSI + MA) and SL, TP ranges:

```
RSI Period: [7, 14, 21]         (3)
RSI Threshold: [60, 70, 70]     (3, but deduped to 2)
MA Period: [10, 20, 30]         (3)
Stop Loss: [3, 5]               (2)
Take Profit: [8, 10, 12]        (3)

Total combos: 3 × 2 × 3 × 2 × 3 = 108
```

**Safety cap**: If total combos exceed 5000, warn the user and allow narrowing ranges.

---

## 8. Server-Side Optimization Execution

### 8.1 Candle Data Reuse

`RunOptimizationAsync` fetches candles **once** at the start and passes them to every backtest iteration. This avoids N+1 database queries.

### 8.2 Parallel Execution

Use `Parallel.ForEach` or a `SemaphoreSlim`-bounded task pool to run combos in parallel:

```csharp
var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
Parallel.ForEach(combinations, parallelOptions, combo =>
{
    var result = RunSingleCombo(session, combo, candles);
    lock (_lock) results.Add(result);
});
```

**Tradeoff**: Parallel execution inside an HTTP request is risky for long-running ops. Two options:
- **Option A (Recommended for initial implementation)**: Run sequentially but report progress. This keeps the architecture simple and is predictable. For typical grids (100–500 combos), it may take 30–120 seconds — acceptable with a progress bar.
- **Option B**: Use a background task (`IHostedService` / `BackgroundService`) with SignalR for real-time push. More complex but truly non-blocking.

**Decision for first pass**: Sequential with progress row-by-row. The HTTP request for start immediately returns a `runId`, and a background `Task.Run` processes combos while the client polls. The `OptimizationRun` entity is updated with progress after each combo.

### 8.3 Strategy Clone Per Combo

Each combo runs on a *copy* of the strategy parameters — the real strategy is never modified until "Apply" is clicked.

```csharp
var testStrategy = CloneStrategyParameters(strategy, combo);
session.Strategy = testStrategy;
var result = await _backtestService.RunBacktestAsync(session);
```

---

## 9. UI Details — Animations & Design

### 9.1 Optimize Button on Details Page

In `Details.cshtml`, next to the existing "Run Backtest" button and badge:

```html
<a asp-action="Optimize" asp-route-id="@Model.Id" class="btn btn-blue" style="padding: 0.75rem 1.5rem; border-radius: 8px; font-weight: 600;">
    ⚡ Optimize
</a>
```

### 9.2 Progress Animation on Optimize Page

- **Gear icon rotation** next to the status text while running.
- **Progress bar** fills smoothly using CSS `transition: width 0.3s ease`.
- **Staggered fade-in** of the result card using the existing `fadeInUp` keyframe.
- **Checkmark animation** when complete: a green circle checkmark scales in.

### 9.3 Apply Button UX

- Shows a brief loading spinner, then displays a toast "Optimized parameters applied!".
- The badge on the session updates to "✓ Optimized".
- The form fields on the strategy details page would reflect the new values on next visit.

---

## 10. Edge Cases & Error Handling

| Scenario | Handling |
|----------|----------|
| No indicators or comparisons on strategy | Show error: "Strategy has no indicators to optimize" |
| User cancels navigation during optimization | Polling interval clears on page unload; server run continues in background |
| All combos produce identical scores | Pick the first one (they're equivalent) |
| Some combos throw exceptions (e.g., not enough data) | Catch per-combo, assign score = 0, continue |
| User clicks Optimize twice | Disable button after first click, show status |
| Combination count > 5000 | Client-side validation warns before submission |
| Browser closes during optimization | Server still runs to completion (background) — next visit shows stale result or status |

---

## 11. Implementation Order

```
Phase 1 — Foundation
  [ ] Create OptimizationRun + OptimizationResult models
  [ ] Add EF migration and update DbContext
  [ ] Create OptimizationService (expand grid, score, single-combo runner)
  [ ] Register new services in Program.cs

Phase 2 — API Endpoints
  [ ] Create OptimizationApiController (start, status, result, apply)
  [ ] Add DTOs for request/response
  [ ] Wire up polling endpoint with progress tracking

Phase 3 — Optimize Configuration Page
  [ ] Create Optimize.cshtml with sweep range form
  [ ] Pre-populate with sensible defaults based on current strategy
  [ ] Add client-side combination count estimate

Phase 4 — Progress & Results UI
  [ ] Create optimization-flow.js (polling, progress bar, result rendering)
  [ ] Create _OptimizationResultPartial.cshtml
  [ ] Add CSS animations for progress bar and completion

Phase 5 — Integration
  [ ] Add Optimize button to Session Details page
  [ ] Implement Apply functionality (strategy update + session flag)
  [ ] Toast notifications for success/failure
  [ ] Disable button during run, handle edge cases
```

---

## 12. Open Questions / Decisions Needed Before Implementation

1. **Sequential vs parallel combo execution?** Sequential is simpler and safer for a first pass. The progress bar keeps it tolerable.
2. **Should the winning combo auto-run a backtest after applying?** Yes — applying should also trigger a `RunBacktestAsync` so the metrics visible on the session reflect the new parameters immediately.
3. **Should optimization results persist permanently or be cleaned up?** Persist permanently so users can review past optimization runs.
4. **Should we allow the user to choose which metrics to optimize for?** For v1, the fixed composite score formula is fine. We can add customizable weights later.
