namespace CryptoBacktestingDashboard.Models.DTO
{
    // Polling response for GET /api/optimization/{runId}/status
    public class OptimizationStatusDTO
    {
        public int RunId { get; set; }
        public string Status { get; set; } = "running"; // running | completed | failed
        public int Completed { get; set; }
        public int Total { get; set; }
        public double BestScoreSoFar { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
