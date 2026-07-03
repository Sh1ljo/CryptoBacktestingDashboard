using CryptoBacktestingDashboard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CryptoBacktestingDashboard.Mcp
{
    /// <summary>
    /// The MCP server runs without a logged-in browser session, but the app's data is
    /// scoped per user. This resolves a single "acting" user once and caches the id:
    /// the email configured under <c>Mcp:UserEmail</c>, falling back to the first user
    /// in the database.
    /// </summary>
    public class McpUserResolver
    {
        private readonly IConfiguration _config;
        private readonly ILogger<McpUserResolver> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private string? _cachedUserId;
        private bool _resolved;

        public McpUserResolver(IConfiguration config, ILogger<McpUserResolver> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string?> GetUserIdAsync(IServiceProvider scopedServices, CancellationToken ct)
        {
            if (_resolved)
                return _cachedUserId;

            await _gate.WaitAsync(ct);
            try
            {
                if (_resolved)
                    return _cachedUserId;

                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                var email = _config["Mcp:UserEmail"];

                var user = !string.IsNullOrWhiteSpace(email)
                    ? await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
                    : null;

                user ??= await context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync(ct);

                _cachedUserId = user?.Id;
                _resolved = true;

                if (_cachedUserId is null)
                    _logger.LogWarning("MCP could not resolve an acting user (none in the database).");
                else
                    _logger.LogInformation("MCP acting as user {Email} ({Id}).", user!.Email, _cachedUserId);

                return _cachedUserId;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
