using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Shopee;

public sealed class ShopeeClient : IShopeeClient
{
    private const string TokenGetPath = "/api/v2/auth/token/get";
    private const string AccessTokenGetPath = "/api/v2/auth/access_token/get";
    private const string OrderListPath = "/api/v2/order/get_order_list";
    private const string OrderDetailPath = "/api/v2/order/get_order_detail";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly ILogger<ShopeeClient> logger;
    private readonly ShopeeOptions options;
    private readonly IShopeeSigner signer;

    public ShopeeClient(
        HttpClient httpClient,
        IOptions<ShopeeOptions> options,
        IShopeeSigner signer,
        ILogger<ShopeeClient> logger)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
        this.signer = signer;
        this.logger = logger;
    }

    public Task<ShopeeTokenResponse> ExchangeCodeAsync(long shopId, string code, CancellationToken cancellationToken = default)
    {
        var path = TokenGetPath;
        var timestamp = UnixNow();
        var query = SignedQuery(path, timestamp);
        var body = new { code, shop_id = shopId, partner_id = options.PartnerId };

        return SendAsync<ShopeeTokenResponse>(HttpMethod.Post, path, query, body, cancellationToken);
    }

    public Task<ShopeeTokenResponse> RefreshAccessTokenAsync(long shopId, string refreshToken, CancellationToken cancellationToken = default)
    {
        var path = AccessTokenGetPath;
        var timestamp = UnixNow();
        var query = SignedQuery(path, timestamp);
        var body = new { refresh_token = refreshToken, shop_id = shopId, partner_id = options.PartnerId };

        return SendAsync<ShopeeTokenResponse>(HttpMethod.Post, path, query, body, cancellationToken);
    }

    public Task<ShopeeOrderListResponse> GetOrderListAsync(
        ShopeeShopCredentials shop,
        DateTimeOffset from,
        DateTimeOffset to,
        string status,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        var path = OrderListPath;
        var timestamp = UnixNow();
        var query = SignedQuery(path, timestamp, shop.AccessToken, shop.ShopId);
        query["time_range_field"] = "create_time";
        query["time_from"] = from.ToUnixTimeSeconds().ToString();
        query["time_to"] = to.ToUnixTimeSeconds().ToString();
        query["page_size"] = options.PageSize.ToString();
        query["order_status"] = status;

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            query["cursor"] = cursor;
        }

        return SendAsync<ShopeeOrderListResponse>(HttpMethod.Get, path, query, null, cancellationToken);
    }

    public async Task<ShopeeOrderDetail?> GetOrderDetailAsync(
        ShopeeShopCredentials shop,
        string orderSn,
        CancellationToken cancellationToken = default)
    {
        var path = OrderDetailPath;
        var timestamp = UnixNow();
        var query = SignedQuery(path, timestamp, shop.AccessToken, shop.ShopId);
        query["order_sn_list"] = orderSn;
        query["response_optional_fields"] = "buyer_username,recipient_address,item_list";

        var response = await SendAsync<ShopeeOrderDetailResponse>(HttpMethod.Get, path, query, null, cancellationToken);
        return response.Orders.FirstOrDefault();
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        Dictionary<string, string> query,
        object? body,
        CancellationToken cancellationToken)
    {
        var uriBuilder = new UriBuilder(new Uri(httpClient.BaseAddress!, path))
        {
            Query = string.Join('&', query.Select(pair =>
                $"{WebUtility.UrlEncode(pair.Key)}={WebUtility.UrlEncode(pair.Value)}"))
        };

        using var request = new HttpRequestMessage(method, uriBuilder.Uri);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response.StatusCode, $"Shopee returned HTTP {(int)response.StatusCode} for {path}.");
        }

        ShopeeApiEnvelope<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ShopeeApiEnvelope<T>>(content, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ShopeeApiException(ShopeeApiErrorKind.InvalidResponse, $"Shopee returned invalid JSON for {path}: {exception.Message}");
        }

        if (envelope is null)
        {
            throw new ShopeeApiException(ShopeeApiErrorKind.InvalidResponse, $"Shopee returned an empty response for {path}.");
        }

        if (!string.IsNullOrWhiteSpace(envelope.Error))
        {
            logger.LogWarning("Shopee API error on {Path}: {Error} - {Message}", path, envelope.Error, envelope.Message);
            throw ToException(envelope.Error, envelope.Message ?? envelope.Error);
        }

        return envelope.Response
            ?? throw new ShopeeApiException(ShopeeApiErrorKind.InvalidResponse, $"Shopee response for {path} did not include a response payload.");
    }

    private Dictionary<string, string> SignedQuery(string path, long timestamp, string? accessToken = null, long? shopId = null)
    {
        var query = new Dictionary<string, string>
        {
            ["partner_id"] = options.PartnerId.ToString(),
            ["timestamp"] = timestamp.ToString(),
            ["sign"] = signer.Sign(path, timestamp, accessToken, shopId)
        };

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            query["access_token"] = accessToken;
        }

        if (shopId.HasValue)
        {
            query["shop_id"] = shopId.Value.ToString();
        }

        return query;
    }

    private static long UnixNow()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private static ShopeeApiException ToException(HttpStatusCode statusCode, string message)
    {
        return statusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError
            ? new ShopeeApiException(ShopeeApiErrorKind.RateLimitOrTransient, message)
            : new ShopeeApiException(ShopeeApiErrorKind.Unknown, message);
    }

    private static ShopeeApiException ToException(string error, string message)
    {
        var normalized = error.ToLowerInvariant();

        if (normalized.Contains("auth") || normalized.Contains("token"))
        {
            return new ShopeeApiException(ShopeeApiErrorKind.Authentication, message);
        }

        if (normalized.Contains("rate") || normalized.Contains("too_many") || normalized.Contains("system_busy"))
        {
            return new ShopeeApiException(ShopeeApiErrorKind.RateLimitOrTransient, message);
        }

        if (normalized.Contains("param") || normalized.Contains("invalid"))
        {
            return new ShopeeApiException(ShopeeApiErrorKind.InvalidResponse, message);
        }

        return new ShopeeApiException(ShopeeApiErrorKind.Unknown, message);
    }
}
