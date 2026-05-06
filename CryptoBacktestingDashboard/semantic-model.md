# Semantic DB Model

This document outlines the database model for the Crypto Backtesting Dashboard application.

## Tables

### BacktestResult
- **Id**: int (Primary Key)
- **BacktestSessionId**: int (Foreign Key to BacktestSession)
- **TradeType**: int (Enum: Long, Short)
- **EntryTime**: datetime
- **ExitTime**: datetime
- **EntryPrice**: decimal
- **ExitPrice**: decimal
- **Quantity**: decimal
- **Commission**: decimal
- **IsWinningTrade**: bool

### BacktestSession
- **Id**: int (Primary Key)
- **StrategyId**: int (Foreign Key to BacktestStrategy)
- **CryptoPairId**: int (Foreign Key to CryptoPair)
- **StartDate**: datetime
- **EndDate**: datetime
- **ExecutedAt**: datetime
- **InitialBalance**: decimal
- **FinalBalance**: decimal
- **IsOptimized**: bool

### BacktestStrategy
- **Id**: int (Primary Key)
- **Name**: string
- **Description**: string
- **IsActive**: bool
- **InitialCapital**: decimal
- **LookbackPeriod**: int
- **CreatedAt**: datetime
- **LastModifiedAt**: datetime?
- **RiskManagementId**: int (Foreign Key to RiskManagement)

### CandleData
- **Id**: int (Primary Key)
- **CryptoPairId**: int (Foreign Key to CryptoPair)
- **Open**: decimal
- **High**: decimal
- **Low**: decimal
- **Close**: decimal
- **Volume**: decimal
- **OpenTime**: datetime
- **CloseTime**: datetime

### CryptoPair
- **Id**: int (Primary Key)
- **Symbol**: string
- **BaseAsset**: string
- **QuoteAsset**: string
- **CurrentPrice**: decimal
- **CreatedAt**: datetime

### Indicator
- **Id**: int (Primary Key)
- **Name**: string
- **Type**: int (Enum: RSI, MACD, etc.)
- **Period**: int
- **Threshold**: decimal
- **Description**: string
- **CreatedAt**: datetime

### RiskManagement
- **Id**: int (Primary Key)
- **Name**: string
- **Type**: int (Enum: StopLoss, TakeProfit, etc.)
- **Value**: decimal
- **Description**: string
- **CreatedAt**: datetime

## Relationships

- **BacktestSession** 1--* **BacktestResult**
- **BacktestStrategy** 1--* **BacktestSession**
- **CryptoPair** 1--* **BacktestSession**
- **CryptoPair** 1--* **CandleData**
- **RiskManagement** 1--* **BacktestStrategy**
- **BacktestStrategy** *--* **Indicator**
