# Lab 1 - Objektni Model i LINQ Upiti

**Datum: 3. travnja 2026.**

## Pregled Implementacije

Projekt **Crypto Backtesting Dashboard** je kompleksna ASP.NET aplikacija za testiranje strategija trgovanja kriptovalutama. Lab 1 implementacija uključuje sve tražene zahtjeve.

---

## 1. OBJEKTNI MODEL (7+ klasa)

### Kompleksne klase (>5 svojstava):

#### 1. **CryptoPair** (5 svojstava + 2 relacije)

- `Id` (int) - identifikator
- `Symbol` (string) - npr. BTC/USD
- `BaseAsset` (string) - npr. BTC
- `QuoteAsset` (string) - npr. USD
- `CurrentPrice` (decimal) - trenutna cijena
- `CreatedAt` (DateTime) - vremensko svojstvo
- **Relacije**: 1-N s CandleData, N-N s BacktestSession

#### 2. **BacktestStrategy** (7 svojstava + 3 relacije)

- `Id` (int)
- `Name` (string)
- `Description` (string)
- `IsActive` (bool)
- `InitialCapital` (decimal)
- `LookbackPeriod` (int)
- `CreatedAt` (DateTime)
- `LastModifiedAt` (DateTime?)
- **Relacije**: N-N s Indicator, 1-N s BacktestSession, 1-1 s RiskManagement

#### 3. **BacktestSession** (9 svojstava + relacije)

- `Id` (int)
- `StrategyId` (int)
- `CryptoPairId` (int)
- `StartDate` (DateTime)
- `EndDate` (DateTime)
- `ExecutedAt` (DateTime)
- `InitialBalance` (decimal)
- `FinalBalance` (decimal)
- `IsOptimized` (bool)
- **Relacije**: 1-N s BacktestResult, N-1 s BacktestStrategy, N-1 s CryptoPair

#### 4. **BacktestResult** (9 svojstava + relacije)

- `Id` (int)
- `BacktestSessionId` (int)
- `TradeType` (enum TradeType) - Long/Short
- `EntryTime` (DateTime)
- `ExitTime` (DateTime)
- `EntryPrice` (decimal)
- `ExitPrice` (decimal)
- `Quantity` (decimal)
- `Commission` (decimal)
- `IsWinningTrade` (bool)
- **Relacije**: N-1 s BacktestSession

#### 5. **Indicator** (5 svojstava + 1 relacija)

- `Id` (int)
- `Name` (string)
- `Type` (enum IndicatorType) - **CUSTOM ENUM**
- `Period` (int)
- `Threshold` (decimal)
- `Description` (string)
- `CreatedAt` (DateTime)
- **Relacije**: N-N s BacktestStrategy

#### 6. **RiskManagement** (5 svojstava + 1 relacija)

- `Id` (int)
- `Name` (string)
- `Type` (enum RiskManagementType) - **CUSTOM ENUM**
- `Value` (decimal)
- `Description` (string)
- `CreatedAt` (DateTime)
- **Relacije**: 1-N s BacktestStrategy

#### 7. **CandleData** (8 svojstava + relacija)

- `Id` (int)
- `CryptoPairId` (int)
- `Open` (decimal)
- `High` (decimal)
- `Low` (decimal)
- `Close` (decimal)
- `Volume` (decimal)
- `OpenTime` (DateTime)
- `CloseTime` (DateTime)
- **Relacije**: N-1 s CryptoPair

---

## 2. CUSTOM ENUMERI

### IndicatorType

```csharp
public enum IndicatorType
{
    RSI,
    MACD,
    MovingAverage,
    BollingerBands,
    Stochastic,
    ATR
}
```

### RiskManagementType

```csharp
public enum RiskManagementType
{
    StopLoss,
    TakeProfit,
    TrailingStop,
    FixedPositionSize,
    PercentageRisk
}
```

### TradeType

```csharp
public enum TradeType
{
    Long,
    Short
}
```

---

## 3. INICIJALIZACIJA PODATAKA

Program inicijalizira **3 glavna objekta** s populiranim podacima:

### Crypto Parovi:

1. **BTC/USD** - 50 candle data točaka
2. **ETH/USD** - 40 candle data točaka
3. **SOL/USD** - 35 candle data točaka

### Strategije:

1. **Mean Reversion Strategy** ($10,000 kapitala)
2. **Trend Following Strategy** ($15,000 kapitala)
3. **Volatility Breakout Strategy** ($8,000 kapitala)

### Backtest Sesije:

1. BTC Mean Reversion Session - 25 upršanih tradova
2. ETH Trend Following Session - 35 upršanih tradova
3. SOL Volatility Session - 18 upršanih tradova

**Ukupno inicijalizovano**: 3 glavna crypto para, 4 indikatora, 3 risk management pravila, 3 strategije, 3 backtest sesije, 78 upršanih tradova.

---

## 4. LINQ UPITI (7 različitih upita)

### Query 1: Profitabilne Backtest Sesije

```csharp
var profitableSessions = sessions
    .Where(s => s.GetROI() > 10m)
    .OrderByDescending(s => s.GetROI())
    .ToList();
```

**Opis**: Filtrira sesije sa ROI > 10% i sortira po dobitima.

### Query 2: Strategije s Win Rate Statističkom

```csharp
var strategyStats = strategies
    .SelectMany(s => s.BacktestSessions, (strategy, session) => new { strategy, session })
    .GroupBy(x => x.strategy.Name)
    .Select(g => new { ... })
    .ToList();
```

**Opis**: Analiza win rate svakog strategije koristeći GroupBy i SelectMany.

### Query 3: Indikatori u Profitabilnim Strategijama

```csharp
var indicatorsInProfitable = strategies
    .Where(s => s.BacktestSessions.Any(bs => bs.GetROI() > 15m))
    .SelectMany(s => s.Indicators)
    .Distinct()
    .OrderBy(i => i.Name)
    .ToList();
```

**Opis**: Pronalazi indikatora korištene samo u profitabilnim strategijama (N-N relacija).

### Query 4: Najbolji Pair

```csharp
var bestPair = pairs
    .Select(p => new { Pair = p, TotalProfit = p.BacktestSessions.Sum(s => s.GetProfit()) })
    .OrderByDescending(x => x.TotalProfit)
    .FirstOrDefault();
```

**Opis**: Pronalazi crypto pair sa najvećim ukupnim profitom (1-N relacija).

### Query 5: Korištenje Risk Management Pravila

```csharp
var rmUsage = strategies
    .GroupBy(s => s.RiskManagement.Name)
    .Select(g => new { Name = g.Key, Count = g.Count() })
    .OrderByDescending(x => x.Count)
    .ToList();
```

**Opis**: Broji korištenje svake risk management strategije.

### Query 6: Sesije s Više od 20 Tradova

```csharp
var activeSessions = sessions
    .Where(s => s.Results.Count > 20)
    .Select(s => new { Session = s, AvgProfitPerTrade = s.Results.Average(r => r.GetProfit()) })
    .OrderByDescending(x => x.AvgProfitPerTrade)
    .ToList();
```

**Opis**: Pronalazi aktivne sesije i računa prosječan profit po tradu.

### Query 7: Analiza Volatilnosti

```csharp
var volatilityAnalysis = pairs
    .Select(p => new { Pair = p, AvgVolatility = p.CandleDataHistory.Average(...) })
    .OrderByDescending(x => x.AvgVolatility)
    .ToList();
```

**Opis**: Analiza prosječne volatilnosti za svaki pair.

---

## 5. ASYNC-AWAIT KONCEPTI

Projekt koristi `async/await` za simulaciju asinkronih backtest operacija:

```csharp
static async Task ExecuteAsyncOperations(List<BacktestStrategy> strategies, List<BacktestSession> sessions)
{
    var tasks = strategies.Select(strategy => SimulateBacktestAsync(strategy)).ToList();
    await Task.WaitAll(tasks);
}

static async Task SimulateBacktestAsync(BacktestStrategy strategy)
{
    Console.WriteLine($"[ASYNC] Starting backtest for '{strategy.Name}' with ${strategy.InitialCapital} capital...");
    await Task.Delay(300 + strategy.Id * 100);
    Console.WriteLine($"[ASYNC] Completed '{strategy.Name}' - Estimated Profit: ${profit} (ROI: {roi:F2}%)");
}
```

**Koncepti**:

- **Task** - predstavlja asinkronu operaciju
- **async/await** - omogućava pisanje asinkronog koda kao sinkronog
- **Task.WhenAll** - čeka sve taskove paralelno
- Simuliraju se duže operacije (npr. pozivi API-ja, baza podataka)

---

## 6. RELACIJE U OBJEKTNOM MODELU

### 1-N Relacije:

- CryptoPair → CandleData (1 pair ima više candle data točaka)
- BacktestStrategy → BacktestSession (1 strategija koristi se u više sesija)
- BacktestSession → BacktestResult (1 sesija ima više upršanih tradova)
- RiskManagement → BacktestStrategy (1 risk rule koristi se u više strategija)

### N-N Relacije:

- BacktestStrategy ↔ Indicator (1 strategija koristi više indikatora, 1 indikator koristi se u više strategija)
- BacktestStrategy ↔ CryptoPair (1 strategija testira se na više paira, 1 pair testira se s više strategija)

---

## 7. DATOTEČNA STRUKTURA

```
Models/Crypto/
├── CryptoPair.cs
├── CandleData.cs
├── Indicator.cs
├── RiskManagement.cs
├── BacktestStrategy.cs
├── BacktestSession.cs
└── BacktestResult.cs

Program.cs (Main programa sa inicijalizacijom i LINQ upitima)
```

---

## 8. REZULTATI POKRETANJA

Program ispisuje:

1. **Inicijalizovane podatke** - sve objekte sa relacijama
2. **7 LINQ upita** - različite analize podataka
3. **Asinkrone operacije** - paralelna izvršavanja backtest simulacija

---

## 9. ZAHTJEVI LAB-1 - CHECKLIST

- [x] Objektni model sa 7+ klasa
- [x] 4+ kompleksne klase sa >5 svojstava
- [x] Custom enum (3 enuma: IndicatorType, RiskManagementType, TradeType)
- [x] DateTime svojstva (CreatedAt, ExecutedAt, LastModifiedAt, itd.)
- [x] 1-N i N-N relacije
- [x] 3 glavna objekta inicijalizovana sa podacima
- [x] Sve sa > 3 sub-objekta svaki
- [x] 7 smislenih LINQ upita
- [x] LINQ upiti su razumljivi i modificibilni pomoću AI-ja
- [x] Razumijevanje async-await koncepata
- [x] Kod je spreman za GitHub
