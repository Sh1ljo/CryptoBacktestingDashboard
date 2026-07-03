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
    public class BacktestStrategyApiControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BacktestStrategyApiControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("BacktestStrategyApiTests");
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

        private async Task<BacktestStrategy> SeedStrategyAsync(ApplicationDbContext db, string name = "Test Strategy")
        {
            var strategy = new BacktestStrategy
            {
                AppUserId = TestAuthHandler.UserId,
                Name = name,
                Description = "Seeded for tests",
                IsActive = true,
                InitialCapital = 10000,
                StopLossPercent = 5m,
                TakeProfitPercent = 10m
            };
            db.BacktestStrategies.Add(strategy);
            await db.SaveChangesAsync();
            return strategy;
        }

        [Fact]
        public async Task Get_ShouldReturnAllStrategies_WhenAnonymous()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedStrategyAsync(db, "List Strategy");

            var response = await _client.GetAsync("/api/strategies");

            response.EnsureSuccessStatusCode();
            var items = await response.Content.ReadFromJsonAsync<List<BacktestStrategyDTO>>();
            Assert.NotNull(items);
            Assert.Contains(items, s => s.Name == "List Strategy");
        }

        [Fact]
        public async Task Get_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var response = await _client.GetAsync("/api/strategies/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldReturnUnauthorized_WhenAnonymous()
        {
            var dto = new BacktestStrategyDTO { Name = "Anon Strategy", Description = "x", IsActive = true, StopLossPercent = 5, TakeProfitPercent = 10 };

            var response = await _client.PostAsJsonAsync("/api/strategies", dto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldReturnBadRequest_WhenNameMissing()
        {
            var client = AuthorizedClient("Admin");
            var dto = new BacktestStrategyDTO { Description = "x", IsActive = true, StopLossPercent = 5, TakeProfitPercent = 10 };

            var response = await client.PostAsJsonAsync("/api/strategies", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldCreateStrategy_WhenAuthorized()
        {
            var client = AuthorizedClient("User");
            var dto = new BacktestStrategyDTO { Name = "New Strategy", Description = "desc", IsActive = true, StopLossPercent = 5, TakeProfitPercent = 10 };

            var response = await client.PostAsJsonAsync("/api/strategies", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<BacktestStrategyDTO>();
            Assert.NotNull(created);
            Assert.True(created!.Id > 0);
            Assert.Equal("New Strategy", created.Name);
        }

        [Fact]
        public async Task Put_ShouldUpdateStrategy_WhenAuthorized()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var strategy = await SeedStrategyAsync(db, "Old Strategy");

            var client = AuthorizedClient("Admin");
            var dto = new BacktestStrategyDTO { Id = strategy.Id, Name = "Updated Strategy", Description = "updated", IsActive = false, StopLossPercent = 3, TakeProfitPercent = 6 };

            var response = await client.PutAsJsonAsync($"/api/strategies/{strategy.Id}", dto);

            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<BacktestStrategyDTO>();
            Assert.NotNull(updated);
            Assert.Equal("Updated Strategy", updated!.Name);
        }

        [Fact]
        public async Task Put_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var client = AuthorizedClient("Admin");
            var dto = new BacktestStrategyDTO { Id = 999999, Name = "Missing", Description = "x", IsActive = true, StopLossPercent = 5, TakeProfitPercent = 10 };

            var response = await client.PutAsJsonAsync("/api/strategies/999999", dto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldReturnForbidden_WhenUserRole()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var strategy = await SeedStrategyAsync(db, "Protected Strategy");

            var client = AuthorizedClient("User");

            var response = await client.DeleteAsync($"/api/strategies/{strategy.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldRemoveStrategy_WhenAdmin()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var strategy = await SeedStrategyAsync(db, "Deletable Strategy");

            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync($"/api/strategies/{strategy.Id}");

            response.EnsureSuccessStatusCode();

            var getResponse = await _client.GetAsync($"/api/strategies/{strategy.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync("/api/strategies/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
