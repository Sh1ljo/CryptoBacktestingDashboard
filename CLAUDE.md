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

- **Models** (`Models/Crypto/`): Domain entities. `BacktestSession` has `GetProfit()` and `GetROI()` computed methods.
- **Repositories** (`Repositories/EF/`): One async repository per entity, injected as scoped services. All follow the same `GetItemsAsync / GetItemAsync / InsertItemAsync / UpdateItemAsync / DeleteItemAsync` interface. Mock repositories exist under `Repositories/` but are unused — only the EF variants are registered.
- **Controllers**: Route-attribute-based, not convention-based. Each entity has its own controller with a `[Route("...")]` prefix (see `sitemap.md` for the full URL map).
- **Views** (`Views/{ControllerName}/`): Razor views with Bootstrap styling. Use ASP.NET Tag Helpers (`asp-for`, `asp-validation-for`, etc.).

### Domain model relationships

```
RiskManagement  ──1:N──▶  BacktestStrategy  ──N:N──▶  Indicator
                                │
                               1:N
                                ▼
CryptoPair  ──────1:N──────  BacktestSession  ──1:N──▶  BacktestResult
    │
   1:N
    ▼
CandleData
```

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
- `BacktestStrategy`: `Indicators`, `BacktestSessions`
- `CryptoPair`: `CandleDataHistory`, `BacktestSessions`
- `Indicator`: `Strategies`
- `RiskManagement`: `Strategies`

### View patterns

See `CryptoBacktestingDashboard/Views/SKILL.md` and `CryptoBacktestingDashboard/.github/skills/edit-form.md` for detailed guidance on creating list pages and edit/create forms.

### CRUD Status

All entities have complete Create, Read, Update, Delete functionality:

- **BacktestSession** - Full CRUD with search on Strategy/Pair symbol ✓
- **BacktestStrategy** - Full CRUD with Risk Management and Indicator multi-select ✓
- **CryptoPair** - Full CRUD with search on Symbol ✓
- **Indicator** - Full CRUD with search on Name/Description ✓
- **RiskManagement** - Full CRUD with search on Name/Description ✓

Each entity follows the same pattern:

1. **Index** - List with AJAX search, + New button, edit/delete action buttons
2. **Create** - Form with validation and proper ModelState cleanup
3. **Edit** - Form with pre-populated data and timestamp preservation
4. **Delete** - AJAX-enabled with confirmation dialog
