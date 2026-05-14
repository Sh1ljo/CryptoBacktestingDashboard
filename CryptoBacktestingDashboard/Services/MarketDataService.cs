using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace CryptoBacktestingDashboard.Services
{
    public class MarketDataService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MarketDataService> _logger;

        public MarketDataService(HttpClient httpClient, ApplicationDbContext context, ILogger<MarketDataService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
        }

        public async Task<(int inserted, string error)> FetchCandlesAsync(int pairId, string interval = "1d", int daysBack = 365)
        {
            var pair = await _context.CryptoPairs.FindAsync(pairId);
            if (pair == null)
                return (0, "Crypto pair not found.");

            var binanceSymbol = pair.Symbol?
                .Replace("/", "")
                .Replace("-", "")
                .ToUpperInvariant();

            if (string.IsNullOrEmpty(binanceSymbol))
                return (0, "Invalid symbol.");

            // Always map to USDT pairs on Binance (e.g., BTC/USD -> BTCUSDT)
            // Strip any known quote suffix first, then append USDT
            foreach (var quote in new[] { "USDT", "USD", "BTC", "ETH" })
            {
                if (binanceSymbol.EndsWith(quote) && binanceSymbol.Length > quote.Length)
                {
                    binanceSymbol = binanceSymbol[..^quote.Length];
                    break;
                }
            }
            binanceSymbol += "USDT";

            var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var startTime = DateTimeOffset.UtcNow.AddDays(-daysBack).ToUnixTimeMilliseconds();

            var url = $"https://api.binance.com/api/v3/klines?symbol={binanceSymbol}&interval={interval}&startTime={startTime}&endTime={endTime}&limit=1000";

            _logger.LogInformation("Fetching from: {Url}", url);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                return (0, $"Network error: {ex.Message}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (0, $"Binance API error ({response.StatusCode}): {body}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var candles = JsonSerializer.Deserialize<List<List<JsonElement>>>(json);

            if (candles == null || candles.Count == 0)
                return (0, "No data returned from Binance.");

            // Get existing timestamps to avoid duplicates
            var existingList = await _context.CandleData
                .Where(c => c.CryptoPairId == pairId)
                .Select(c => c.OpenTime)
                .ToListAsync();

            var existingTimestamps = new HashSet<DateTime>(existingList);

            var newCandles = new List<CandleData>();
            foreach (var c in candles)
            {
                var openTime = DateTimeOffset.FromUnixTimeMilliseconds(c[0].GetInt64()).UtcDateTime;

                if (existingTimestamps.Contains(openTime))
                    continue;

                newCandles.Add(new CandleData
                {
                    CryptoPairId = pairId,
                    OpenTime = openTime,
                    CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(c[6].GetInt64()).UtcDateTime,
                    Open = decimal.Parse(c[1].GetString()!, CultureInfo.InvariantCulture),
                    High = decimal.Parse(c[2].GetString()!, CultureInfo.InvariantCulture),
                    Low = decimal.Parse(c[3].GetString()!, CultureInfo.InvariantCulture),
                    Close = decimal.Parse(c[4].GetString()!, CultureInfo.InvariantCulture),
                    Volume = decimal.Parse(c[5].GetString()!, CultureInfo.InvariantCulture)
                });
            }

            if (newCandles.Count == 0)
                return (0, "All candles already exist in the database.");

            _context.CandleData.AddRange(newCandles);
            await _context.SaveChangesAsync();

            // Update pair's current price to the latest close
            var latest = await _context.CandleData
                .Where(c => c.CryptoPairId == pairId)
                .OrderByDescending(c => c.OpenTime)
                .FirstOrDefaultAsync();

            if (latest != null)
            {
                pair.CurrentPrice = latest.Close;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Inserted {Count} candles for {Symbol}, price: ${Price}", newCandles.Count, pair.Symbol, pair.CurrentPrice);
            return (newCandles.Count, string.Empty);
        }

        /// <summary>
        /// Calculates the price change percentage for a crypto pair based on current price and price from 24h ago.
        /// </summary>
        /// <param name="pairId">The ID of the crypto pair</param>
        /// <returns>Price change percentage (positive or negative) or null if insufficient data</returns>
        public async Task<decimal?> GetPriceChangePercentageAsync(int pairId)
        {
            // Get the latest candle (current price is the close of latest candle)
            var latestCandle = await _context.CandleData
                .Where(c => c.CryptoPairId == pairId)
                .OrderByDescending(c => c.OpenTime)
                .FirstOrDefaultAsync();

            if (latestCandle == null)
                return null;

            // Get the close price from approximately 24 hours ago
            // Since we have daily candles, we need the most recent candle that is at least 24h old
            var currentClose = latestCandle.Close;

            // Calculate the timestamp for 24 hours ago from latest candle's close time
            var twentyFourHoursAgo = latestCandle.CloseTime.AddHours(-24);

            // Find the candle that was active around that time (the one with OpenTime closest to but not after twentyFourHoursAgo)
            var previousCandle = await _context.CandleData
                .Where(c => c.CryptoPairId == pairId && c.OpenTime <= twentyFourHoursAgo)
                .OrderByDescending(c => c.OpenTime)
                .FirstOrDefaultAsync();

            if (previousCandle == null)
                return null;

            var previousClose = previousCandle.Close;

            // Calculate percentage change
            if (previousClose == 0)
                return null;

            var change = ((currentClose - previousClose) / previousClose) * 100;
            return change;
        }
    }
}