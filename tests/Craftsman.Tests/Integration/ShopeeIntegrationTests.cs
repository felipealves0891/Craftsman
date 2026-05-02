using System.Net;
using System.Text.Json;
using Craftsman.Domain.Integration.Models;
using Craftsman.Domain.Integration.Services;
using Craftsman.Infra.Integrations.Shopee;
using Craftsman.Infra.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Craftsman.Tests.Integration;

public sealed class ShopeeIntegrationTests
{
    [Fact]
    public void Mapper_creates_raw_order_for_variant_and_plain_items()
    {
        var detail = new ShopeeOrderDetail(
            "250428ABC",
            "comprador",
            new ShopeeRecipientAddress("Destinatario"),
            [
                new ShopeeOrderItem(10, "Caneca", 20, "Azul", 2, 35.5m, 40m),
                new ShopeeOrderItem(11, "Chaveiro", 0, null, 1, 0m, 12m)
            ]);

        var rawOrder = ShopeeOrderMapper.ToRawOrder(detail);

        Assert.Equal("Shopee", rawOrder.Source);
        Assert.Equal("250428ABC", rawOrder.ExternalOrderId);
        Assert.Collection(
            rawOrder.Items,
            item =>
            {
                Assert.Equal("20", item.ExternalItemId);
                Assert.Equal("Caneca - Azul", item.Description);
                Assert.Equal(2, item.Quantity);
                Assert.Equal(35.5m, item.UnitPrice);
            },
            item =>
            {
                Assert.Equal("11", item.ExternalItemId);
                Assert.Equal("Chaveiro", item.Description);
                Assert.Equal(1, item.Quantity);
                Assert.Equal(12m, item.UnitPrice);
            });
    }

    [Fact]
    public async Task Token_service_refreshes_expiring_access_token()
    {
        var repository = new FakeShopeeTokenRepository();
        var service = new ShopeeTokenService(
            new FakeShopeeClient(new ShopeeTokenResponse("new-access", "new-refresh", 3600)),
            repository,
            Options.Create(new ShopeeOptions { TokenRefreshSkewMinutes = 10 }));

        var refreshed = await service.EnsureValidAccessTokenAsync(new ShopeeShopCredentials(
            123,
            "old-access",
            "old-refresh",
            DateTimeOffset.UtcNow.AddMinutes(1),
            ShopeeShopAuthorizationStatus.Active));

        Assert.Equal("new-access", refreshed.AccessToken);
        Assert.Equal("new-refresh", refreshed.RefreshToken);
        Assert.Equal(123, repository.SavedShopId);
    }

    [Fact]
    public async Task Token_service_marks_reauthorization_when_refresh_fails()
    {
        var repository = new FakeShopeeTokenRepository();
        var service = new ShopeeTokenService(
            new FakeShopeeClient(new ShopeeApiException(ShopeeApiErrorKind.Authentication, "invalid refresh token")),
            repository,
            Options.Create(new ShopeeOptions { TokenRefreshSkewMinutes = 10 }));

        await Assert.ThrowsAsync<ShopeeApiException>(() => service.EnsureValidAccessTokenAsync(new ShopeeShopCredentials(
            123,
            "old-access",
            "old-refresh",
            DateTimeOffset.UtcNow.AddMinutes(-1),
            ShopeeShopAuthorizationStatus.Active)));

        Assert.Equal(123, repository.ReauthorizationShopId);
    }

    [Fact]
    public async Task Order_source_uses_http_client_pagination_details_and_skips_partial_detail_error()
    {
        var handler = new FakeShopeeHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://partner.test")
        };
        var options = Options.Create(new ShopeeOptions
        {
            Enabled = true,
            BaseUrl = "https://partner.test",
            PartnerId = 100,
            PartnerKey = "secret",
            PageSize = 1,
            SearchWindowHours = 24,
            AllowedStatuses = ["READY_TO_SHIP"]
        });
        var client = new ShopeeClient(httpClient, options, new ShopeeSigner(options), NullLogger<ShopeeClient>.Instance);
        var source = new ShopeeOrderSource(
            new FakeShopeeTokenRepository([
                new ShopeeShopCredentials(321, "access", "refresh", DateTimeOffset.UtcNow.AddHours(1), ShopeeShopAuthorizationStatus.Active)
            ]),
            new PassThroughShopeeTokenService(),
            client,
            options,
            NullLogger<ShopeeOrderSource>.Instance);

        var orders = await source.FetchOrdersAsync();

        var order = Assert.Single(orders);
        Assert.Equal("250428OK", order.ExternalOrderId);
        Assert.Equal(2, handler.OrderListCalls);
        Assert.Equal(2, handler.DetailCalls);
    }

    [Fact]
    public async Task Pipeline_records_shopee_source_failure_without_blocking_other_sources()
    {
        var pipeline = new OrderImportPipeline(
            [new ThrowingOrderSource(), new StaticOrderSource()],
            new OrderNormalizer(),
            new FakeOrderRepository(),
            new FakeProductMappingRepository(),
            new FakeUnitOfWork(),
            new FakeDomainEventPublisher());

        var result = await pipeline.ImportAsync();

        Assert.Equal(1, result.ImportedCount);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("Shopee", failure.Source);
    }

    [Fact]
    public async Task Pipeline_deduplicates_shopee_order_sn()
    {
        var pipeline = new OrderImportPipeline(
            [new DuplicateShopeeOrderSource()],
            new OrderNormalizer(),
            new FakeOrderRepository(),
            new FakeProductMappingRepository(),
            new FakeUnitOfWork(),
            new FakeDomainEventPublisher());

        var result = await pipeline.ImportAsync();

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task Repository_saves_and_loads_protected_tokens()
    {
        await using var dbContext = CreateDbContext();
        var repository = new ShopeeShopTokenRepository(dbContext, new EphemeralDataProtectionProvider());

        await repository.SaveTokensAsync(555, "plain-access", "plain-refresh", DateTimeOffset.UtcNow.AddHours(1));

        var entity = await dbContext.ShopeeShops.SingleAsync();
        Assert.NotEqual("plain-access", entity.AccessToken);
        Assert.NotEqual("plain-refresh", entity.RefreshToken);

        var loaded = await repository.GetByShopIdAsync(555);
        Assert.NotNull(loaded);
        Assert.Equal("plain-access", loaded.AccessToken);
        Assert.Equal("plain-refresh", loaded.RefreshToken);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private sealed class FakeShopeeHandler : HttpMessageHandler
    {
        public int DetailCalls { get; private set; }

        public int OrderListCalls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath;
            if (path == "/api/v2/order/get_order_list")
            {
                OrderListCalls++;
                var query = request.RequestUri?.Query ?? string.Empty;
                var body = query.Contains("cursor=next", StringComparison.Ordinal)
                    ? new
                    {
                        response = new
                        {
                            more = false,
                            next_cursor = "",
                            order_list = new[] { new { order_sn = "250428FAIL" } }
                        }
                    }
                    : new
                    {
                        response = new
                        {
                            more = true,
                            next_cursor = "next",
                            order_list = new[] { new { order_sn = "250428OK" } }
                        }
                    };

                return Json(body, HttpStatusCode.OK);
            }

            if (path == "/api/v2/order/get_order_detail")
            {
                DetailCalls++;
                var query = request.RequestUri?.Query ?? string.Empty;
                if (query.Contains("250428FAIL", StringComparison.Ordinal))
                {
                    return Json(new { error = "error_param", message = "detail failed" }, HttpStatusCode.OK);
                }

                return Json(new
                {
                    response = new
                    {
                        order_list = new[]
                        {
                            new
                            {
                                order_sn = "250428OK",
                                buyer_username = "buyer",
                                recipient_address = new { name = "recipient" },
                                item_list = new[]
                                {
                                    new
                                    {
                                        item_id = 1,
                                        item_name = "Produto",
                                        model_id = 2,
                                        model_name = "P",
                                        model_quantity_purchased = 1,
                                        model_discounted_price = 10m,
                                        model_original_price = 12m
                                    }
                                }
                            }
                        }
                    }
                }, HttpStatusCode.OK);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static Task<HttpResponseMessage> Json(object body, HttpStatusCode statusCode)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(JsonSerializer.Serialize(body))
            });
        }
    }

    private sealed class FakeShopeeClient : IShopeeClient
    {
        private readonly Exception? exception;
        private readonly ShopeeTokenResponse? tokenResponse;

        public FakeShopeeClient(ShopeeTokenResponse tokenResponse)
        {
            this.tokenResponse = tokenResponse;
        }

        public FakeShopeeClient(Exception exception)
        {
            this.exception = exception;
        }

        public Task<ShopeeTokenResponse> ExchangeCodeAsync(long shopId, string code, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ShopeeTokenResponse> RefreshAccessTokenAsync(long shopId, string refreshToken, CancellationToken cancellationToken = default)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(tokenResponse!);
        }

        public Task<ShopeeOrderListResponse> GetOrderListAsync(ShopeeShopCredentials shop, DateTimeOffset from, DateTimeOffset to, string status, string? cursor, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ShopeeOrderDetail?> GetOrderDetailAsync(ShopeeShopCredentials shop, string orderSn, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeShopeeTokenRepository : IShopeeShopTokenRepository
    {
        private readonly IReadOnlyCollection<ShopeeShopCredentials> shops;

        public FakeShopeeTokenRepository()
            : this([])
        {
        }

        public FakeShopeeTokenRepository(IReadOnlyCollection<ShopeeShopCredentials> shops)
        {
            this.shops = shops;
        }

        public long? ReauthorizationShopId { get; private set; }

        public long? SavedShopId { get; private set; }

        public ShopeeShopCredentials? SavedCredentials { get; private set; }

        public Task<IReadOnlyCollection<ShopeeShopCredentials>> ListAuthorizedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(shops);
        }

        public Task<ShopeeShopCredentials?> GetByShopIdAsync(long shopId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SavedCredentials);
        }

        public Task SaveTokensAsync(long shopId, string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAt, CancellationToken cancellationToken = default)
        {
            SavedShopId = shopId;
            SavedCredentials = new ShopeeShopCredentials(shopId, accessToken, refreshToken, accessTokenExpiresAt, ShopeeShopAuthorizationStatus.Active);
            return Task.CompletedTask;
        }

        public Task MarkReauthorizationRequiredAsync(long shopId, CancellationToken cancellationToken = default)
        {
            ReauthorizationShopId = shopId;
            return Task.CompletedTask;
        }
    }

    private sealed class PassThroughShopeeTokenService : IShopeeTokenService
    {
        public Task<ShopeeShopCredentials> AuthorizeAsync(long shopId, string code, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ShopeeShopCredentials> EnsureValidAccessTokenAsync(ShopeeShopCredentials shop, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(shop);
        }
    }

    private sealed class ThrowingOrderSource : IOrderSource
    {
        public string SourceName => "Shopee";

        public Task<IReadOnlyCollection<RawOrder>> FetchOrdersAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Shopee unavailable");
        }
    }

    private sealed class StaticOrderSource : IOrderSource
    {
        public string SourceName => "Manual";

        public Task<IReadOnlyCollection<RawOrder>> FetchOrdersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<RawOrder>>([
                new RawOrder("Manual", "M-1", [new RawOrderItem("SKU", "Produto", 1, 10m)])
            ]);
        }
    }

    private sealed class DuplicateShopeeOrderSource : IOrderSource
    {
        public string SourceName => "Shopee";

        public Task<IReadOnlyCollection<RawOrder>> FetchOrdersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<RawOrder>>([
                new RawOrder("Shopee", "250428DUP", [new RawOrderItem("SKU", "Produto", 1, 10m)]),
                new RawOrder("Shopee", "250428DUP", [new RawOrderItem("SKU", "Produto", 1, 10m)])
            ]);
        }
    }

    private sealed class FakeOrderRepository : Craftsman.Domain.Sales.Repositories.IOrderRepository
    {
        private readonly List<Craftsman.Domain.Sales.Entities.Order> orders = [];

        public Task AddAsync(Craftsman.Domain.Sales.Entities.Order order, CancellationToken cancellationToken = default)
        {
            orders.Add(order);
            return Task.CompletedTask;
        }

        public Task<Craftsman.Domain.Sales.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(orders.FirstOrDefault(order => order.Id == id));
        }

        public Task<Craftsman.Domain.Sales.Entities.Order?> GetByOriginAsync(Craftsman.Domain.Sales.ObjectValues.OrderOrigin origin, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(orders.FirstOrDefault(order => order.Origin.Source == origin.Source && order.Origin.ExternalOrderId == origin.ExternalOrderId));
        }

        public Task<IReadOnlyCollection<Craftsman.Domain.Sales.Entities.Order>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Craftsman.Domain.Sales.Entities.Order>>(orders.AsReadOnly());
        }

        public Task UpdateAsync(Craftsman.Domain.Sales.Entities.Order order, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductMappingRepository : Craftsman.Domain.ProductCatalog.Repositories.IProductMappingRepository
    {
        public Task AddAsync(Craftsman.Domain.ProductCatalog.Entities.ProductMapping mapping, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<Craftsman.Domain.ProductCatalog.Entities.ProductMapping?> GetByExternalItemAsync(string source, string externalItemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Craftsman.Domain.ProductCatalog.Entities.ProductMapping?>(null);
        }

        public Task<IReadOnlyCollection<Craftsman.Domain.ProductCatalog.Entities.ProductMapping>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Craftsman.Domain.ProductCatalog.Entities.ProductMapping>>(Array.Empty<Craftsman.Domain.ProductCatalog.Entities.ProductMapping>());
        }
    }

    private sealed class FakeUnitOfWork : Craftsman.Domain.Repositories.IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeDomainEventPublisher : Craftsman.Domain.Events.IDomainEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
            where TEvent : Craftsman.Domain.Events.IDomainEvent
        {
            return Task.CompletedTask;
        }
    }
}
