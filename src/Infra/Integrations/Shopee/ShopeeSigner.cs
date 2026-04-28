using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Shopee;

public interface IShopeeSigner
{
    string Sign(string path, long timestamp, string? accessToken = null, long? shopId = null);
}

public sealed class ShopeeSigner : IShopeeSigner
{
    private readonly ShopeeOptions options;

    public ShopeeSigner(IOptions<ShopeeOptions> options)
    {
        this.options = options.Value;
    }

    public string Sign(string path, long timestamp, string? accessToken = null, long? shopId = null)
    {
        var baseString = new StringBuilder()
            .Append(options.PartnerId)
            .Append(path)
            .Append(timestamp);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            baseString.Append(accessToken);
        }

        if (shopId.HasValue)
        {
            baseString.Append(shopId.Value);
        }

        var key = Encoding.UTF8.GetBytes(options.PartnerKey);
        var payload = Encoding.UTF8.GetBytes(baseString.ToString());
        var hash = HMACSHA256.HashData(key, payload);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
