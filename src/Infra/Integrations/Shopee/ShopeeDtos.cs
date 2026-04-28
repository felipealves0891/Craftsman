using System.Text.Json.Serialization;

namespace Craftsman.Infra.Integrations.Shopee;

public sealed record ShopeeTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expire_in")] int ExpiresIn);

public sealed record ShopeeOrderListResponse(
    [property: JsonPropertyName("more")] bool More,
    [property: JsonPropertyName("next_cursor")] string? NextCursor,
    [property: JsonPropertyName("order_list")] List<ShopeeOrderSummary> Orders);

public sealed record ShopeeOrderSummary([property: JsonPropertyName("order_sn")] string OrderSn);

public sealed record ShopeeOrderDetailResponse(
    [property: JsonPropertyName("order_list")] List<ShopeeOrderDetail> Orders);

public sealed record ShopeeOrderDetail(
    [property: JsonPropertyName("order_sn")] string OrderSn,
    [property: JsonPropertyName("buyer_username")] string? BuyerUsername,
    [property: JsonPropertyName("recipient_address")] ShopeeRecipientAddress? RecipientAddress,
    [property: JsonPropertyName("item_list")] List<ShopeeOrderItem> Items);

public sealed record ShopeeRecipientAddress([property: JsonPropertyName("name")] string? Name);

public sealed record ShopeeOrderItem(
    [property: JsonPropertyName("item_id")] long ItemId,
    [property: JsonPropertyName("item_name")] string ItemName,
    [property: JsonPropertyName("model_id")] long ModelId,
    [property: JsonPropertyName("model_name")] string? ModelName,
    [property: JsonPropertyName("model_quantity_purchased")] int ModelQuantityPurchased,
    [property: JsonPropertyName("model_discounted_price")] decimal ModelDiscountedPrice,
    [property: JsonPropertyName("model_original_price")] decimal ModelOriginalPrice);

internal sealed record ShopeeApiEnvelope<T>(
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("response")] T? Response);
