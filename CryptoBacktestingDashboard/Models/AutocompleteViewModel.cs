namespace CryptoBacktestingDashboard.Models
{
    public class AutocompleteViewModel
    {
        public string PropertyName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SearchUrl { get; set; } = string.Empty;
        public string? InitialText { get; set; }
        public int? InitialValue { get; set; }
    }
}