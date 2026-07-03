using System.Collections.Generic;

namespace CryptoBacktestingDashboard.Models.DTO
{
    /// <summary>
    /// Represents a single search result item returned by the global search.
    /// </summary>
    public class SearchResultItem
    {
        public string Type { get; set; } = "";  // "Menu", "Page", "Strategy", "Session", "Pair", "Indicator"
        public string Label { get; set; } = "";
        public string? Description { get; set; }
        public string Url { get; set; } = "";
        public string? Badge { get; set; }   // e.g., "Active", "RSI", "BTC/USD"
    }

    /// <summary>
    /// Wrapper returned by the search API.
    /// </summary>
    public class SearchResultDTO
    {
        public List<SearchResultItem> Results { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
