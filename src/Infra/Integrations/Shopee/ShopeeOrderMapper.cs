using Craftsman.Domain.Integration.Models;

namespace Craftsman.Infra.Integrations.Shopee;

public static class ShopeeOrderMapper
{
    public static RawOrder ToRawOrder(ShopeeOrderDetail detail)
    {
        var items = detail.Items.Select(item =>
        {
            var externalItemId = item.ModelId > 0 ? item.ModelId.ToString() : item.ItemId.ToString();
            var description = string.IsNullOrWhiteSpace(item.ModelName)
                ? item.ItemName
                : $"{item.ItemName} - {item.ModelName}";
            var unitPrice = item.ModelDiscountedPrice > 0 ? item.ModelDiscountedPrice : item.ModelOriginalPrice;

            return new RawOrderItem(
                externalItemId,
                description,
                item.ModelQuantityPurchased,
                unitPrice);
        }).ToList();

        return new RawOrder("Shopee", detail.OrderSn, items.AsReadOnly());
    }
}
