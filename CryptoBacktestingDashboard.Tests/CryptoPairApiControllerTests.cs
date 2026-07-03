using System.Net;
using System.Net.Http.Json;
using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using Microsoft.AspNetCore.Authentication;
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
                        options.UseInMemoryDatabase("CryptoPairApiTests");
                    });

                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        private HttpClient AuthorizedClient(string role)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
            return client;
        }

        private async Task<CryptoPair> SeedPairAsync(ApplicationDbContext db, string symbol = "TEST/USD")
        {
            var pair = new CryptoPair { Symbol = symbol, BaseAsset = "TEST", QuoteAsset = "USD", CurrentPrice = 123.45m, CreatedAt = DateTime.Now };
            db.CryptoPairs.Add(pair);
            await db.SaveChangesAsync();
            return pair;
        }

        [Fact]
        public async Task Get_ShouldReturnAllPairs_WhenAnonymous()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedPairAsync(db, "TEST/USD");

            var response = await _client.GetAsync("/api/pairs");

            response.EnsureSuccessStatusCode();
            var pairs = await response.Content.ReadFromJsonAsync<List<CryptoPairDTO>>();
            Assert.NotNull(pairs);
            Assert.Contains(pairs, p => p.Symbol == "TEST/USD");
        }

        [Fact]
        public async Task Get_ShouldReturnPair_WhenIdExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pair = await SeedPairAsync(db, "GETBYID/USD");

            var response = await _client.GetAsync($"/api/pairs/{pair.Id}");

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<CryptoPairDTO>();
            Assert.NotNull(dto);
            Assert.Equal("GETBYID/USD", dto!.Symbol);
        }

        [Fact]
        public async Task Get_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var response = await _client.GetAsync("/api/pairs/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldReturnUnauthorized_WhenAnonymous()
        {
            var newPair = new CryptoPairDTO { Symbol = "ERR/USD", BaseAsset = "ERR", QuoteAsset = "USD", CurrentPrice = 0 };

            var response = await _client.PostAsJsonAsync("/api/pairs", newPair);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldReturnBadRequest_WhenSymbolMissing()
        {
            var client = AuthorizedClient("Admin");
            var invalidPair = new CryptoPairDTO { BaseAsset = "ERR", QuoteAsset = "USD", CurrentPrice = 0 };

            var response = await client.PostAsJsonAsync("/api/pairs", invalidPair);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldCreatePair_WhenAuthorized()
        {
            var client = AuthorizedClient("User");
            var newPair = new CryptoPairDTO { Symbol = "NEW/USD", BaseAsset = "NEW", QuoteAsset = "USD", CurrentPrice = 1.5m };

            var response = await client.PostAsJsonAsync("/api/pairs", newPair);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<CryptoPairDTO>();
            Assert.NotNull(created);
            Assert.True(created!.Id > 0);
            Assert.Equal("NEW/USD", created.Symbol);
        }

        [Fact]
        public async Task Put_ShouldUpdatePair_WhenAuthorized()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pair = await SeedPairAsync(db, "OLD/USD");

            var client = AuthorizedClient("Admin");
            var updated = new CryptoPairDTO { Id = pair.Id, Symbol = "UPDATED/USD", BaseAsset = "UPD", QuoteAsset = "USD", CurrentPrice = 99m };

            var response = await client.PutAsJsonAsync($"/api/pairs/{pair.Id}", updated);

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<CryptoPairDTO>();
            Assert.NotNull(dto);
            Assert.Equal("UPDATED/USD", dto!.Symbol);
        }

        [Fact]
        public async Task Delete_ShouldReturnForbidden_WhenUserRole()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pair = await SeedPairAsync(db, "NODELETE/USD");

            var client = AuthorizedClient("User");

            var response = await client.DeleteAsync($"/api/pairs/{pair.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldRemovePair_WhenAdmin()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pair = await SeedPairAsync(db, "DELETEME/USD");

            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync($"/api/pairs/{pair.Id}");

            response.EnsureSuccessStatusCode();

            var getResponse = await _client.GetAsync($"/api/pairs/{pair.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task Put_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var client = AuthorizedClient("Admin");
            var dto = new CryptoPairDTO { Id = 999999, Symbol = "MISSING/USD", BaseAsset = "MIS", QuoteAsset = "USD", CurrentPrice = 1 };

            var response = await client.PutAsJsonAsync("/api/pairs/999999", dto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync("/api/pairs/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
