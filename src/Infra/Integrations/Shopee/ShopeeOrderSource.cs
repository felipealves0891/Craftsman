using Craftsman.Domain.Integration.Models;
using Craftsman.Domain.Integration.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Shopee;

public sealed class ShopeeOrderSource : IOrderSource
{
    private readonly IShopeeClient client;
    private readonly ILogger<ShopeeOrderSource> logger;
    private readonly ShopeeOptions options;
    private readonly IShopeeShopTokenRepository tokenRepository;
    private readonly IShopeeTokenService tokenService;

    public ShopeeOrderSource(
        IShopeeShopTokenRepository tokenRepository,
        IShopeeTokenService tokenService,
        IShopeeClient client,
        IOptions<ShopeeOptions> options,
        ILogger<ShopeeOrderSource> logger)
    {
        this.tokenRepository = tokenRepository;
        this.tokenService = tokenService;
        this.client = client;
        this.options = options.Value;
        this.logger = logger;
    }

    public string SourceName => "Shopee";

    public async Task<IReadOnlyCollection<RawOrder>> FetchOrdersAsync(CancellationToken cancellationToken = default)
    {
        var shops = await tokenRepository.ListAuthorizedAsync(cancellationToken);
        var orders = new List<RawOrder>();
        var failures = new List<Exception>();
        var to = DateTimeOffset.UtcNow;
        var from = to.AddHours(-options.SearchWindowHours);

        foreach (var shop in shops)
        {
            try
            {
                var currentShop = await tokenService.EnsureValidAccessTokenAsync(shop, cancellationToken);

                foreach (var status in options.AllowedStatuses)
                {
                    await FetchShopStatusAsync(currentShop, from, to, status, orders, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Shopee import failed for shop {ShopId}.", shop.ShopId);
                failures.Add(exception);
            }
        }

        if (orders.Count == 0 && failures.Count > 0)
        {
            throw new InvalidOperationException(string.Join("; ", failures.Select(failure => failure.Message)));
        }

        return orders.AsReadOnly();
    }

    private async Task FetchShopStatusAsync(
        ShopeeShopCredentials shop,
        DateTimeOffset from,
        DateTimeOffset to,
        string status,
        List<RawOrder> orders,
        CancellationToken cancellationToken)
    {
        string? cursor = null;

        do
        {
            var page = await client.GetOrderListAsync(shop, from, to, status, cursor, cancellationToken);

            foreach (var summary in page.Orders)
            {
                try
                {
                    var detail = await client.GetOrderDetailAsync(shop, summary.OrderSn, cancellationToken);
                    if (detail is not null)
                    {
                        orders.Add(ShopeeOrderMapper.ToRawOrder(detail));
                    }
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Shopee order detail import failed for order {OrderSn}.", summary.OrderSn);
                }
            }

            cursor = page.More ? page.NextCursor : null;
        }
        while (!string.IsNullOrWhiteSpace(cursor));
    }
}
