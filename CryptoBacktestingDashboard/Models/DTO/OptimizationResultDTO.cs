using System.Collections.Generic;

namespace CryptoBacktestingDashboard.Models.DTO
{
    // A single human-readable parameter for display, e.g. "RSI Period" -> "14"
    public class ParamDisplayItem
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
    }

    // One "best by X" candidate the user can choose to apply. The optimization
    // tracks the top combo for each criterion (composite score, profit, profit
    // factor, win rate, drawdown) and returns them all so the user picks which
    // trade-off they want.
    public class OptimizationOptionDTO
    {
        // Stable key used by the Apply call: "composite" | "profit" | "profitFactor" | "winRate" | "drawdown"
        public string Key { get; set; } = "";
        // Human label, e.g. "Highest Profit"
        public string Label { get; set; } = "";
        // The persisted OptimizationResult row this option applies.
        public int ResultId { get; set; }

        public double Score { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitPercent { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal WinRate { get; set; }
        public decimal MaxDrawdownPercent { get; set; }
        public int TotalTrades { get; set; }
        public decimal SharpeApprox { get; set; }

        public List<ParamDisplayItem> ParamDisplay { get; set; } = new();
    }

    // Response for GET /api/optimization/{runId}/result
    public class OptimizationResultDTO
    {
        public int RunId { get; set; }
        public int TotalCombinations { get; set; }

        // One option per criterion, in display order. The first entry is the
        // composite-score winner (the default recommendation).
        public List<OptimizationOptionDTO> Options { get; set; } = new();

        public bool HasOptions => Options.Count > 0;
    }
}
