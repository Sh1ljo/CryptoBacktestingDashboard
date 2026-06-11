namespace CryptoBacktestingDashboard.Models.DTO
{
    public class CryptoPairDTO
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string BaseAsset { get; set; }
        public string QuoteAsset { get; set; }
        public decimal? CurrentPrice { get; set; }
    }
}