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
    public class IndicatorApiControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public IndicatorApiControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("IndicatorApiTests");
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

        private async Task<Indicator> SeedIndicatorAsync(ApplicationDbContext db, string name = "Test RSI")
        {
            var indicator = new Indicator
            {
                Name = name,
                Type = IndicatorType.RSI,
                Period = 14,
                Threshold = 70,
                Description = "Seeded for tests"
            };
            db.Indicators.Add(indicator);
            await db.SaveChangesAsync();
            return indicator;
        }

        [Fact]
        public async Task Get_ShouldReturnAllIndicators_WhenAnonymous()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedIndicatorAsync(db, "List RSI");

            var response = await _client.GetAsync("/api/indicators");

            response.EnsureSuccessStatusCode();
            var items = await response.Content.ReadFromJsonAsync<List<IndicatorDTO>>();
            Assert.NotNull(items);
            Assert.Contains(items, i => i.Name == "List RSI");
        }

        [Fact]
        public async Task Get_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var response = await _client.GetAsync("/api/indicators/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldReturnUnauthorized_WhenAnonymous()
        {
            var dto = new IndicatorDTO { Name = "Anon RSI", Type = IndicatorType.RSI, Period = 14, Threshold = 70 };

            var response = await _client.PostAsJsonAsync("/api/indicators", dto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldReturnBadRequest_WhenNameMissing()
        {
            var client = AuthorizedClient("Admin");
            var dto = new IndicatorDTO { Type = IndicatorType.RSI, Period = 14, Threshold = 70 };

            var response = await client.PostAsJsonAsync("/api/indicators", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_ShouldCreateIndicator_WhenAuthorized()
        {
            var client = AuthorizedClient("User");
            var dto = new IndicatorDTO { Name = "New RSI", Type = IndicatorType.RSI, Period = 14, Threshold = 70, Description = "desc" };

            var response = await client.PostAsJsonAsync("/api/indicators", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<IndicatorDTO>();
            Assert.NotNull(created);
            Assert.True(created!.Id > 0);
            Assert.Equal("New RSI", created.Name);
        }

        [Fact]
        public async Task Put_ShouldUpdateIndicator_WhenAuthorized()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var indicator = await SeedIndicatorAsync(db, "Old RSI");

            var client = AuthorizedClient("Admin");
            var dto = new IndicatorDTO { Id = indicator.Id, Name = "Updated RSI", Type = IndicatorType.RSI, Period = 21, Threshold = 80, Description = "updated" };

            var response = await client.PutAsJsonAsync($"/api/indicators/{indicator.Id}", dto);

            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<IndicatorDTO>();
            Assert.NotNull(updated);
            Assert.Equal("Updated RSI", updated!.Name);
            Assert.Equal(21, updated.Period);
        }

        [Fact]
        public async Task Put_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var client = AuthorizedClient("Admin");
            var dto = new IndicatorDTO { Id = 999999, Name = "Missing", Type = IndicatorType.RSI, Period = 14, Threshold = 70 };

            var response = await client.PutAsJsonAsync("/api/indicators/999999", dto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldReturnForbidden_WhenUserRole()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var indicator = await SeedIndicatorAsync(db, "Protected RSI");

            var client = AuthorizedClient("User");

            var response = await client.DeleteAsync($"/api/indicators/{indicator.Id}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldRemoveIndicator_WhenAdmin()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var indicator = await SeedIndicatorAsync(db, "Deletable RSI");

            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync($"/api/indicators/{indicator.Id}");

            response.EnsureSuccessStatusCode();

            var getResponse = await _client.GetAsync($"/api/indicators/{indicator.Id}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenIdDoesNotExist()
        {
            var client = AuthorizedClient("Admin");

            var response = await client.DeleteAsync("/api/indicators/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
