using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Domain.Shipping.Services;
using Craftsman.Infra.Integrations.Loggi;
using Craftsman.Infra.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Craftsman.Tests.Shipping;

public sealed class LoggiShippingTrackerTests
{
    [Theory]
    [InlineData(1, ShipmentStatus.Created)]
    [InlineData(28, ShipmentStatus.Created)]
    [InlineData(3, ShipmentStatus.InTransit)]
    [InlineData(15, ShipmentStatus.InTransit)]
    [InlineData(30, ShipmentStatus.InTransit)]
    [InlineData(12, ShipmentStatus.DeliveryAttempted)]
    [InlineData(18, ShipmentStatus.DeliveryAttempted)]
    [InlineData(22, ShipmentStatus.DeliveryAttempted)]
    [InlineData(5, ShipmentStatus.Delivered)]
    [InlineData(2, ShipmentStatus.DeliveryAttempted)]
    [InlineData(9, ShipmentStatus.DeliveryAttempted)]
    [InlineData(29, ShipmentStatus.DeliveryAttempted)]
    public void Mapper_converts_loggi_status_code_to_shipment_status(int statusCode, ShipmentStatus expectedStatus)
    {
        var status = LoggiTrackingStatusMapper.ToShipmentStatus(new LoggiTrackingEvent(
            "LOGGI123",
            statusCode,
            null,
            DateTimeOffset.UtcNow));

        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public async Task Token_service_requests_oauth_v2_token_and_reuses_cached_token()
    {
        var handler = new FakeLoggiTokenHandler(HttpStatusCode.OK, new
        {
            access_token = "token-123",
            expires_in = 3600
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.loggi.test")
        };
        var options = Options.Create(new LoggiOptions
        {
            Enabled = true,
            BaseUrl = "https://api.loggi.test",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            CompanyId = "company-1"
        });
        var service = new LoggiTokenService(httpClient, options);

        var firstToken = await service.GetAccessTokenAsync();
        var secondToken = await service.GetAccessTokenAsync();

        Assert.Equal("token-123", firstToken);
        Assert.Equal("token-123", secondToken);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("/v2/oauth2/token", handler.RequestPath);
        Assert.Contains("\"client_id\":\"client-1\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"client_secret\":\"secret-1\"", handler.RequestBody, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, LoggiApiErrorKind.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, LoggiApiErrorKind.Authorization)]
    public async Task Token_service_classifies_authentication_and_authorization_errors(
        HttpStatusCode statusCode,
        LoggiApiErrorKind expectedKind)
    {
        var handler = new FakeLoggiTokenHandler(statusCode, new { error = "invalid" });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.loggi.test")
        };
        var options = Options.Create(new LoggiOptions
        {
            Enabled = true,
            BaseUrl = "https://api.loggi.test",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            CompanyId = "company-1"
        });
        var service = new LoggiTokenService(httpClient, options);

        var exception = await Assert.ThrowsAsync<LoggiApiException>(() => service.GetAccessTokenAsync());

        Assert.Equal(expectedKind, exception.Kind);
    }

    [Fact]
    public async Task Tracking_client_returns_latest_event_using_bearer_token()
    {
        var handler = new FakeLoggiTrackingHandler(HttpStatusCode.OK, new
        {
            tracking_code = "LOGGI123",
            history = new[]
            {
                new
                {
                    status = new
                    {
                        code = 3,
                        description = "Em rota",
                        updatedTime = "2026-04-28T10:00:00Z"
                    }
                },
                new
                {
                    status = new
                    {
                        code = 5,
                        description = "Entregue",
                        updatedTime = "2026-04-29T12:30:00Z"
                    }
                }
            }
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.loggi.test")
        };
        var options = Options.Create(new LoggiOptions
        {
            Enabled = true,
            BaseUrl = "https://api.loggi.test",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            CompanyId = "company-1"
        });
        var client = new LoggiTrackingClient(
            httpClient,
            new StaticLoggiTokenService("token-1"),
            options,
            NullLogger<LoggiTrackingClient>.Instance);

        var latestEvent = await client.GetLatestEventAsync(" LOGGI123 ");

        Assert.NotNull(latestEvent);
        Assert.Equal("LOGGI123", latestEvent.TrackingCode);
        Assert.Equal(5, latestEvent.StatusCode);
        Assert.Equal(DateTimeOffset.Parse("2026-04-29T12:30:00Z"), latestEvent.UpdatedAt);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("token-1", handler.Authorization?.Parameter);
        Assert.Equal("/v1/companies/company-1/packages/LOGGI123/tracking", handler.RequestPath);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Tracking_client_classifies_rate_limit_and_transient_errors(HttpStatusCode statusCode)
    {
        var handler = new FakeLoggiTrackingHandler(statusCode, new { error = "transient" });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.loggi.test")
        };
        var options = Options.Create(new LoggiOptions
        {
            Enabled = true,
            BaseUrl = "https://api.loggi.test",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            CompanyId = "company-1"
        });
        var client = new LoggiTrackingClient(
            httpClient,
            new StaticLoggiTokenService("token-1"),
            options,
            NullLogger<LoggiTrackingClient>.Instance);

        var exception = await Assert.ThrowsAsync<LoggiApiException>(() => client.GetLatestEventAsync("LOGGI123"));

        Assert.Equal(LoggiApiErrorKind.RateLimitOrTransient, exception.Kind);
    }

    [Fact]
    public async Task Tracking_client_classifies_invalid_json()
    {
        var handler = new FakeLoggiTrackingHandler(HttpStatusCode.OK, "{");
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.loggi.test")
        };
        var options = Options.Create(new LoggiOptions
        {
            Enabled = true,
            BaseUrl = "https://api.loggi.test",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            CompanyId = "company-1"
        });
        var client = new LoggiTrackingClient(
            httpClient,
            new StaticLoggiTokenService("token-1"),
            options,
            NullLogger<LoggiTrackingClient>.Instance);

        var exception = await Assert.ThrowsAsync<LoggiApiException>(() => client.GetLatestEventAsync("LOGGI123"));

        Assert.Equal(LoggiApiErrorKind.InvalidResponse, exception.Kind);
    }

    [Fact]
    public async Task Tracker_returns_latest_event_as_tracked_shipment_status()
    {
        var trackingClient = new StaticLoggiTrackingClient(new LoggiTrackingEvent(
            "LOGGI123",
            5,
            "Entregue",
            DateTimeOffset.Parse("2026-04-29T12:30:00Z")));
        var tracker = new LoggiShippingTracker(trackingClient);

        var tracked = await tracker.TrackAsync(" LOGGI123 ");

        Assert.NotNull(tracked);
        Assert.Equal("LOGGI123", tracked.TrackingCode);
        Assert.Equal(ShipmentStatus.Delivered, tracked.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-04-29T12:30:00Z"), tracked.TrackedAt);
        Assert.Equal("LOGGI123", trackingClient.TrackingCode);
    }

    [Fact]
    public void Infrastructure_uses_loggi_shipping_tracker_when_loggi_is_enabled()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CraftsmanDb"] = "Host=localhost;Database=craftsman;Username=craftsman;Password=craftsman",
                ["Loggi:Enabled"] = "true",
                ["Loggi:BaseUrl"] = "https://api.loggi.test",
                ["Loggi:ClientId"] = "client-1",
                ["Loggi:ClientSecret"] = "secret-1",
                ["Loggi:CompanyId"] = "company-1"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddCraftsmanInfrastructure(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        var tracker = scope.ServiceProvider.GetRequiredService<IShippingTracker>();

        Assert.IsType<LoggiShippingTracker>(tracker);
    }

    private sealed class StaticLoggiTokenService : ILoggiTokenService
    {
        private readonly string token;

        public StaticLoggiTokenService(string token)
        {
            this.token = token;
        }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(token);
        }
    }

    private sealed class StaticLoggiTrackingClient : ILoggiTrackingClient
    {
        private readonly LoggiTrackingEvent trackingEvent;

        public StaticLoggiTrackingClient(LoggiTrackingEvent trackingEvent)
        {
            this.trackingEvent = trackingEvent;
        }

        public string? TrackingCode { get; private set; }

        public Task<LoggiTrackingEvent?> GetLatestEventAsync(string trackingCode, CancellationToken cancellationToken = default)
        {
            TrackingCode = trackingCode;
            return Task.FromResult<LoggiTrackingEvent?>(trackingEvent);
        }
    }

    private sealed class FakeLoggiTokenHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly object responseBody;

        public FakeLoggiTokenHandler(HttpStatusCode statusCode, object responseBody)
        {
            this.statusCode = statusCode;
            this.responseBody = responseBody;
        }

        public int CallCount { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        public string? RequestPath { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            RequestPath = request.RequestUri?.AbsolutePath;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return Json(statusCode, responseBody);
        }
    }

    private sealed class FakeLoggiTrackingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode statusCode;
        private readonly object responseBody;

        public FakeLoggiTrackingHandler(HttpStatusCode statusCode, object responseBody)
        {
            this.statusCode = statusCode;
            this.responseBody = responseBody;
        }

        public AuthenticationHeaderValue? Authorization { get; private set; }

        public string? RequestPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            RequestPath = request.RequestUri?.AbsolutePath;

            return Task.FromResult(Json(statusCode, responseBody));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, object body)
    {
        var content = body is string raw
            ? raw
            : JsonSerializer.Serialize(body);

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        };
    }
}
