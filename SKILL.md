---
description: "Entity Framework (EF) Skill for Database Model and Migration Management"
tags: ["entity-framework", "database", "migrations", "models"]
applyTo:
  - "Models/**"
  - "Data/**"
  - "Migrations/**"
---

# Entity Framework (EF) Skill

**Purpose**: Provides guidance for working with Entity Framework Core models, configurations, migrations, and database operations in the Crypto Backtesting Dashboard project.

## When to Use This Skill

Use this skill when you need to:

- Add or modify EF model classes in `/Models` or `/Models/Crypto`
- Configure model relationships, constraints, or data annotations
- Update `ApplicationDbContext` in `/Data/ApplicationDbContext.cs`
- Generate and apply database migrations
- Seed initial database data
- Add foreign keys, navigation properties, or indexes
- Configure DbSets and model builders

## Project Context

**Framework**: ASP.NET Core 8.0 with Entity Framework Core  
**Database**: SQL Server (LocalDB for development)  
**DbContext**: `CryptoBacktestingDashboard.Data.ApplicationDbContext`

### Current Models (Entities)

Located in `CryptoBacktestingDashboard/Models/Crypto/`:

1. **BacktestResult** - Individual trade results from backtesting
2. **BacktestSession** - A complete backtesting execution
3. **BacktestStrategy** - Trading strategy configuration
4. **CandleData** - OHLCV candle data for crypto pairs
5. **CryptoPair** - Cryptocurrency trading pair (e.g., BTC/USD)
6. **Indicator** - Technical indicators (RSI, MACD, etc.)
7. **RiskManagement** - Risk management rules (Stop Loss, Take Profit)

## Workflow: Adding a New Property to a Model

### Step 1: Modify the Model Class

```csharp
// Example: Models/Crypto/BacktestStrategy.cs
public class BacktestStrategy
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    // NEW PROPERTY - Add it here
    [Range(0.01, 100)]
    public decimal MaxDrawdown { get; set; } = 10m;

    // ... other properties ...
}
```

### Step 2: Create a Migration

```bash
dotnet ef migrations add AddMaxDrawdownToBacktestStrategy
```

Output will be similar to:

```
info: Microsoft.EntityFrameworkCore.Design.DesignTimeLoggerProvider[1]
      Build started...
Build succeeded.
Created migration 'AddMaxDrawdownToBacktestStrategy'.
```

### Step 3: Apply the Migration to Database

```bash
dotnet ef database update
```

Output:

```
info: Microsoft.EntityFrameworkCore.Infrastructure[10403]
      Entity Framework Core initialized 'ApplicationDbContext' using provider 'Microsoft.SqlServer.EntityFrameworkCore'
```

## Data Annotations Guide

Use these annotations for model configuration:

| Annotation                      | Purpose                  | Example                                                        |
| ------------------------------- | ------------------------ | -------------------------------------------------------------- |
| `[Key]`                         | Primary key              | `public int Id { get; set; }`                                  |
| `[Required]`                    | Not null constraint      | `[Required] public string Name { get; set; }`                  |
| `[StringLength(n)]`             | Max string length        | `[StringLength(255)] public string Description { get; set; }`  |
| `[Range(min, max)]`             | Numeric range validation | `[Range(0, 100)] public decimal Percentage { get; set; }`      |
| `[ForeignKey("PropertyName")]`  | Foreign key reference    | `[ForeignKey("Strategy")] public int StrategyId { get; set; }` |
| `[InverseProperty("Property")]` | Inverse navigation       | For two-way relationships                                      |
| `[Table("TableName")]`          | Custom table name        | `[Table("BacktestResults")]`                                   |
| `[Column("ColumnName")]`        | Custom column name       | `[Column("EntryPrice")]`                                       |
| `[DatabaseGenerated(...)]`      | Auto-generated columns   | For computed/default columns                                   |

## Navigation Properties & Relationships

### One-to-Many (1:M)

```csharp
// Parent: BacktestStrategy (1)
public class BacktestStrategy
{
    public int Id { get; set; }
    public virtual ICollection<BacktestSession> Sessions { get; set; } = new List<BacktestSession>();
}

// Child: BacktestSession (Many)
public class BacktestSession
{
    public int Id { get; set; }
    public int StrategyId { get; set; }
    [ForeignKey("StrategyId")]
    public virtual BacktestStrategy Strategy { get; set; }
}
```

### Many-to-Many (M:M)

```csharp
// Use junction/join table
public class StrategyIndicator
{
    public int StrategyId { get; set; }
    public int IndicatorId { get; set; }

    [ForeignKey("StrategyId")]
    public virtual BacktestStrategy Strategy { get; set; }

    [ForeignKey("IndicatorId")]
    public virtual Indicator Indicator { get; set; }
}
```

## Seeding Data in OnModelCreating

Add seed data in `Data/ApplicationDbContext.cs`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<CryptoPair>().HasData(
        new CryptoPair { Id = 1, Symbol = "BTC/USD", BaseAsset = "BTC", QuoteAsset = "USD", CurrentPrice = 60000 },
        new CryptoPair { Id = 2, Symbol = "ETH/USD", BaseAsset = "ETH", QuoteAsset = "USD", CurrentPrice = 2000 }
    );
}
```

## Common EF Commands

```bash
# View migration history
dotnet ef migrations list

# Create migration without applying
dotnet ef migrations add MigrationName

# Apply latest migrations
dotnet ef database update

# Revert to specific migration
dotnet ef database update MigrationNameToRevertTo

# Remove last unapplied migration
dotnet ef migrations remove

# Generate migration script (SQL)
dotnet ef migrations script --output migration.sql
```

## DbContext File Location

**Path**: `CryptoBacktestingDashboard/Data/ApplicationDbContext.cs`

Always ensure:

- All model DbSets are declared as properties
- Relationships are configured in `OnModelCreating`
- Seed data is added for development/testing

## Important Reminders

✅ **DO**:

- Keep navigation properties virtual for lazy loading
- Use `[Required]` for not-null constraints
- Create migrations with descriptive names
- Test migrations locally before committing
- Document complex relationships

❌ **DON'T**:

- Modify migration files manually (they're auto-generated)
- Break existing migrations in production
- Leave unmapped properties on models
- Forget to apply migrations to the database

## Related Files

- Models: `CryptoBacktestingDashboard/Models/Crypto/`
- DbContext: `CryptoBacktestingDashboard/Data/ApplicationDbContext.cs`
- Migrations: `CryptoBacktestingDashboard/Migrations/`
- Program.cs: Database configuration and DI setup
