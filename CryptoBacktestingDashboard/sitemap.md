# Sitemap

This document outlines the URL structure of the Crypto Backtesting Dashboard application.

| URL                | Controller       | Action  | View                                  |
| ------------------ | ---------------- | ------- | ------------------------------------- |
| /                  | Home             | Index   | Views/Home/Index.cshtml               |
| /Home/Privacy      | Home             | Privacy | Views/Home/Privacy.cshtml             |
| /backtests         | BacktestSession  | Index   | Views/BacktestSession/Index.cshtml    |
| /backtests/{id}    | BacktestSession  | Details | Views/BacktestSession/Details.cshtml  |
| /strategies        | BacktestStrategy | Index   | Views/BacktestStrategy/Index.cshtml   |
| /strategies/{id}   | BacktestStrategy | Details | Views/BacktestStrategy/Details.cshtml |
| /pairs             | CryptoPair       | Index   | Views/CryptoPair/Index.cshtml         |
| /pairs/{id}        | CryptoPair       | Details | Views/CryptoPair/Details.cshtml       |
| /indicators        | Indicator        | Index   | Views/Indicator/Index.cshtml          |
| /indicators/{id}   | Indicator        | Details | Views/Indicator/Details.cshtml        |
| /risk              | RiskManagement   | Index   | Views/RiskManagement/Index.cshtml     |
| /risk/{id}         | RiskManagement   | Details | Views/RiskManagement/Details.cshtml   |
| /candles           | CandleData       | Index   | Views/CandleData/Index.cshtml         |
| /candles/{id}      | CandleData       | Details | Views/CandleData/Details.cshtml       |
| /candles/create    | CandleData       | Create  | Views/CandleData/Create.cshtml        |
| /candles/{id}/edit | CandleData       | Edit    | Views/CandleData/Edit.cshtml          |
