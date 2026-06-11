using System.Net;
using System.Net.Http.Json;
using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CryptoBacktestingDashboard.Tests
{
    public class CryptoPairApiControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CryptoPairApiControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        private async Task<CryptoPair> SeedPairAsync(ApplicationDbContext db)
        {
            var pair = new CryptoPair { Symbol = "TEST/USD", BaseAsset = "TEST", QuoteAsset = "USD", CurrentPrice = 123.45m, CreatedAt = DateTime.Now };
            db.CryptoPairs.Add(pair);
            await db.SaveChangesAsync();
            return pair;
        }

        [Fact]
        public async Task Get_ShouldReturnAllPairs_WhenAnonymous()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedPairAsync(db);

            // Act
            var response = await _client.GetAsync("/api/pairs");

            // Assert
            response.EnsureSuccessStatusCode();
            var pairs = await response.Content.ReadFromJsonAsync<List<CryptoPairDTO>>();
            Assert.NotNull(pairs);
            Assert.Contains(pairs, p => p.Symbol == "TEST/USD");
        }

        [Fact]
        public async Task Post_ShouldReturnUnauthorized_WhenAnonymous()
        {
            // Arrange
            var newPair = new CryptoPairDTO { Symbol = "ERR/USD", BaseAsset = "ERR", QuoteAsset = "USD", CurrentPrice = 0 };

            // Act
            var response = await _client.PostAsJsonAsync("/api/pairs", newPair);

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); // Identity redirects to login since Unauthorized (or 401 if API behavior but default is Redirect without config)
        }
    }
}