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
    public class BacktestSessionApiControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BacktestSessionApiControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("BacktestSessionApiTests");
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

        private async Task<(BacktestStrategy Strategy, CryptoPair Pair)> SeedDependenciesAsync(ApplicationDbContext db, string suffix)
        {
            var strategy = new BacktestStrategy
            {
                AppUserId = TestAuthHandler.UserId,
                Name = $"Strategy {suffix}",
                Description = "Seeded for tests",
                IsActive = true,
                InitialCapital = 10000,
                StopLossPercent = 5m,
                TakeProfitPercent = 10m
            };
            var pair = new CryptoPair { Symbol = $"SES{suffix}/USD", BaseAsset = "SES", QuoteAsset = "USD", CurrentPrice = 100m, CreatedAt = DateTime.Now };

            db.BacktestStrategies.Add(strategy);
            db.CryptoPairs.Add(pair);
            await db.SaveChangesAsync();

            return (strategy, pair);
        }

        private async Task<BacktestSession> SeedSessionAsync(ApplicationDbContext db, string suffix)
        {
            var (strategy, pair) = await SeedDependenciesAsync(db, suffix);

            var session = new BacktestSession
            {
                AppUserId = TestAuthHandler.UserId,
                StrategyId = strategy.Id,
                CryptoPairId = pair.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 6, 1),
                InitialBalance = 1000m,
                FinalBalance = 0m
            };
            db.BacktestSessions.Add(session);
            await db.SaveChangesAsync();
            return session;
        }

        [Fact]
        public async Task Get_ShouldReturnAllSessions_WhenAnonymous()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedSessionAsync(db, "List");

            var response = await _client.GetAsync("/api/sessions");

            response.EnsureSuccessStatusCode();
            var items = await response.Content.ReadFromJsonAsync<List<BacktestSessionDTO>>();
            Assert.NotNull(items);
            Assert.Contains(items, s => s.CryptoPair != null && s.CryptoPair.Symbol == "SESList/USD");
        }

        [Fact]
        public async Task Get_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var response = await _client.GetAsync("/api/sessions/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_ShouldReturnSessionWithNestedDtos_WhenIdExists()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await SeedSessionAsync(db, "Nested");

            var response = await _client.GetAsync($"/api/sessions/{session.Id}");

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<BacktestSessionDTO>();
            Assert.NotNull(dto);
            Assert.NotNull(dto!.Strategy);
            Assert.NotNull(dto.CryptoPair);
            Assert.Equal("SESNested/USD", dto.CryptoPair!.Symbol);
        }

        [Fact]
        public async Task Post_ShouldReturnUnauthorized_WhenAnonymous()
        {
            var dto = new BacktestSessionDTO
            {
                StrategyId = 1,
                CryptoPairId = 1,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 6, 1),
                InitialBalance = 1000m
            };

            var response = await _client.PostAsJsonAsync("/api/sessions", dto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldReturnBadRequest_WhenInitialBalanceIsZero()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (strategy, pair) = await SeedDependenciesAsync(db, "BadReq");

            var client = AuthorizedClient("Admin");
            var dto = new BacktestSessionDTO
            {
                StrategyId = strategy.Id,
                CryptoPairId = pair.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 6, 1),
                InitialBalance = 0m
            };

            var response = await client.PostAsJsonAsync("/api/sessions", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldCreateSession_WhenAuthorized()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (strategy, pair) = await SeedDependenciesAsync(db, "Create");

            var client = AuthorizedClient("User");
            var dto = new BacktestSessionDTO
            {
                StrategyId = strategy.Id,
                CryptoPairId = pair.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 6, 1),
                InitialBalance = 5000m
            };

            var response = await client.PostAsJsonAsync("/api/sessions", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<BacktestSessionDTO>();
            Assert.NotNull(created);
            Assert.True(created!.Id > 0);
            Assert.Equal(5000m, created.InitialBalance);
            Assert.Equal(0m, created.Profit);
        }

        [Fact]
        public async Task Put_ShouldUpdateSession_WhenAuthorized()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await SeedSessionAsync(db, "Update");

            var client = AuthorizedClient("Admin");
            var dto = new BacktestSessionDTO
            {
                Id = session.Id,
                StrategyId = session.StrategyId,
                CryptoPairId = session.CryptoPairId,
                StartDate = session.StartDate,
                EndDate = new DateTime(2024, 12, 31),
                InitialBalance = 2000m
            };

            var response = await client.PutAsJsonAsync($"/api/sessions/{session.Id}", dto);

            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<BacktestSessionDTO>();
            Assert.NotNull(updated);
            Assert.Equal(2000m, updated!.InitialBalance);
            Assert.Equal(new DateTime(2024, 12, 31), updated.EndDate);
        }

        [Fact]
        public async Task Put_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var (strategy, pair) = await SeedDependenciesAsync(db, "PutMissing");

            var client = AuthorizedClient("Admin");
            var dto = new BacktestSessionDTO
            {
                Id = 999999,
                StrategyId = strategy.Id,
                CryptoPairId = pair.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 6, 1),
                InitialBalance = 1000m
            };

            var response = await client.PutAsJsonAsync("/api/sessions/999999", dto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync("/api/sessions/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldReturnForbidden_WhenUserRole()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await SeedSessionAsync(db, "Protected");

            var client = AuthorizedClient("User");

            var response = await client.DeleteAsync($"/api/sessions/{session.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldRemoveSession_WhenAdmin()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var session = await SeedSessionAsync(db, "Delete");

            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync($"/api/sessions/{session.Id}");

            response.EnsureSuccessStatusCode();

            var getResponse = await _client.GetAsync($"/api/sessions/{session.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }
}
