using System.Net;
using System.Net.Http.Json;
using CryptoBacktestingDashboard.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CryptoBacktestingDashboard.Tests
{
    public class AiChatApiControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AiChatApiControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("AiChatApiTests");
                    });

                    services.AddAuthentication(TestAuthHandler.SchemeName)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                });
            });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        private HttpClient AuthorizedClient(string role)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
            return client;
        }

        [Fact]
        public async Task Stream_ShouldReturnUnauthorized_WhenAnonymous()
        {
            var response = await _client.PostAsJsonAsync("/api/chat/stream", new { message = "hello" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Stream_ShouldEmitSseEventStream_WhenAuthorized()
        {
            var client = AuthorizedClient("User");

            var response = await client.PostAsJsonAsync("/api/chat/stream", new { message = "hello" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

            var body = await response.Content.ReadAsStringAsync();

            // Each SSE frame is a "data: {...}" line. The no-API-key path emits an error
            // event followed by the closing done event, in that order.
            Assert.Contains("data:", body);
            Assert.Contains("\"type\":\"error\"", body);
            Assert.Contains("\"type\":\"done\"", body);
            Assert.True(body.IndexOf("\"type\":\"error\"") < body.IndexOf("\"type\":\"done\""),
                "The error event should be streamed before the done event.");
        }

        [Fact]
        public async Task Stream_ShouldEmitError_WhenMessageEmpty()
        {
            var client = AuthorizedClient("User");

            var response = await client.PostAsJsonAsync("/api/chat/stream", new { message = "" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"type\":\"error\"", body);
            Assert.Contains("\"type\":\"done\"", body);
        }
    }
}
