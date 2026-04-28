using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Shopee;

public sealed class ShopeeOptions
{
    public const string SectionName = "Shopee";

    public bool Enabled { get; set; }

    public string Environment { get; set; } = "Production";

    public string BaseUrl { get; set; } = "https://partner.shopeemobile.com";

    public long PartnerId { get; set; }

    public string PartnerKey { get; set; } = string.Empty;

    public int SearchWindowHours { get; set; } = 24;

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int PageSize { get; set; } = 50;

    public int TokenRefreshSkewMinutes { get; set; } = 10;

    public List<string> AllowedStatuses { get; set; } = ["READY_TO_SHIP"];
}

public sealed class ShopeeOptionsValidator : IValidateOptions<ShopeeOptions>
{
    public ValidateOptionsResult Validate(string? name, ShopeeOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("Shopee:BaseUrl must be an absolute URL.");
        }

        if (options.PartnerId <= 0)
        {
            failures.Add("Shopee:PartnerId must be configured when Shopee is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.PartnerKey))
        {
            failures.Add("Shopee:PartnerKey must be configured when Shopee is enabled.");
        }

        if (options.SearchWindowHours <= 0)
        {
            failures.Add("Shopee:SearchWindowHours must be greater than zero.");
        }

        if (options.RequestTimeoutSeconds <= 0)
        {
            failures.Add("Shopee:RequestTimeoutSeconds must be greater than zero.");
        }

        if (options.PageSize is <= 0 or > 100)
        {
            failures.Add("Shopee:PageSize must be between 1 and 100.");
        }

        if (options.TokenRefreshSkewMinutes < 0)
        {
            failures.Add("Shopee:TokenRefreshSkewMinutes cannot be negative.");
        }

        if (options.AllowedStatuses.Count == 0 || options.AllowedStatuses.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("Shopee:AllowedStatuses must contain at least one valid status.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
