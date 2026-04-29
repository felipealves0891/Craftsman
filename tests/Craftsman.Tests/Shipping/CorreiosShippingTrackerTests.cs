using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Craftsman.Domain.Shipping.Entities;
using Craftsman.Infra.Integrations.Correios;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Craftsman.Tests.Shipping;

public sealed class CorreiosShippingTrackerTests
{
    [Theory]
    [InlineData("Objeto criado", ShipmentStatus.Created)]
    [InlineData("Objeto postado", ShipmentStatus.InTransit)]
    [InlineData("Objeto em transito", ShipmentStatus.InTransit)]
    [InlineData("Objeto saiu para entrega ao destinatario", ShipmentStatus.InTransit)]
    [InlineData("Tentativa de entrega nao efetuada", ShipmentStatus.DeliveryAttempted)]
    [InlineData("Objeto aguardando retirada no endereco indicado", ShipmentStatus.DeliveryAttempted)]
    [InlineData("Objeto entregue ao destinatario", ShipmentStatus.Delivered)]
    public void Mapper_converts_correios_event_description_to_shipment_status(string description, ShipmentStatus expectedStatus)
    {
        var status = CorreiosTrackingStatusMapper.ToShipmentStatus(new CorreiosTrackingEvent(
            "AA123456789BR",
            null,
            null,
            description,
            DateTimeOffset.UtcNow));

        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public async Task Tracker_returns_latest_event_as_tracked_shipment_status()
    {
        var handler = new FakeCorreiosTrackingHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.correios.test")
        };
        var options = Options.Create(new CorreiosOptions
        {
            Enabled = true,
            BaseUrl = "https://api.correios.test",
            TrackingPathTemplate = "/sro-rastro/v1/objetos/{trackingCode}?resultado=T",
            UserName = "user",
            ApiAccessCode = "access-code"
        });
        var client = new CorreiosTrackingClient(
            httpClient,
            new StaticCorreiosTokenService("token-1"),
            options,
            NullLogger<CorreiosTrackingClient>.Instance);
        var tracker = new CorreiosShippingTracker(client);

        var tracked = await tracker.TrackAsync(" AA123456789BR ");

        Assert.NotNull(tracked);
        Assert.Equal("AA123456789BR", tracked.TrackingCode);
        Assert.Equal(ShipmentStatus.Delivered, tracked.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-04-29T12:30:00Z"), tracked.TrackedAt);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("token-1", handler.Authorization?.Parameter);
        Assert.Equal("/sro-rastro/v1/objetos/AA123456789BR", handler.RequestPath);
        Assert.Equal("?resultado=T", handler.Query);
    }

    [Fact]
    public async Task Token_service_uses_basic_auth_and_contract_endpoint()
    {
        var handler = new FakeCorreiosTokenHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.correios.test")
        };
        var options = Options.Create(new CorreiosOptions
        {
            Enabled = true,
            TokenBaseUrl = "https://api.correios.test",
            UserName = "meu-correios",
            ApiAccessCode = "api-code",
            ContractNumber = "123456",
            ContractDr = 10
        });
        var service = new CorreiosTokenService(httpClient, options);

        var token = await service.GetAccessTokenAsync();

        Assert.Equal("token-123", token);
        Assert.Equal("/token/v1/autentica/contrato", handler.RequestPath);
        Assert.Equal("Basic", handler.Authorization?.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("meu-correios:api-code")),
            handler.Authorization?.Parameter);
        Assert.Contains("\"numero\":\"123456\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"dr\":10", handler.RequestBody, StringComparison.Ordinal);
    }

    private sealed class StaticCorreiosTokenService : ICorreiosTokenService
    {
        private readonly string token;

        public StaticCorreiosTokenService(string token)
        {
            this.token = token;
        }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(token);
        }
    }

    private sealed class FakeCorreiosTrackingHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        public string? Query { get; private set; }

        public string? RequestPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            Query = request.RequestUri?.Query;
            RequestPath = request.RequestUri?.AbsolutePath;

            return Json(new
            {
                objetos = new[]
                {
                    new
                    {
                        codObjeto = "AA123456789BR",
                        eventos = new[]
                        {
                            new
                            {
                                codigo = "PO",
                                tipo = "01",
                                descricao = "Objeto postado",
                                dtHrCriado = "2026-04-28T10:00:00Z"
                            },
                            new
                            {
                                codigo = "BDE",
                                tipo = "01",
                                descricao = "Objeto entregue ao destinatario",
                                dtHrCriado = "2026-04-29T12:30:00Z"
                            }
                        }
                    }
                }
            });
        }
    }

    private sealed class FakeCorreiosTokenHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        public string RequestBody { get; private set; } = string.Empty;

        public string? RequestPath { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            RequestPath = request.RequestUri?.AbsolutePath;
            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return await Json(new
            {
                token = "token-123",
                expiraEm = "2026-04-29T13:30:00Z"
            });
        }
    }

    private static Task<HttpResponseMessage> Json(object body)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body))
        });
    }
}
