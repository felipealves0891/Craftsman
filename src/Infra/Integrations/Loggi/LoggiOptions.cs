using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Loggi;

public sealed class LoggiOptions
{
    public const string SectionName = "Loggi";

    public bool Enabled { get; set; }

    public string Environment { get; set; } = "Production";

    public string BaseUrl { get; set; } = "https://api.loggi.com";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string CompanyId { get; set; } = string.Empty;

    public string TokenPath { get; set; } = "/v2/oauth2/token";

    public string TrackingPathTemplate { get; set; } = "/v1/companies/{companyId}/packages/{trackingCode}/tracking";

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int TokenRefreshSkewMinutes { get; set; } = 5;
}

public sealed class LoggiOptionsValidator : IValidateOptions<LoggiOptions>
{
    public ValidateOptionsResult Validate(string? name, LoggiOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("Loggi:BaseUrl must be an absolute URL.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add("Loggi:ClientId must be configured when Loggi is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            failures.Add("Loggi:ClientSecret must be configured when Loggi is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.CompanyId))
        {
            failures.Add("Loggi:CompanyId must be configured when Loggi is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.TokenPath))
        {
            failures.Add("Loggi:TokenPath must be configured when Loggi is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.TrackingPathTemplate)
            || !options.TrackingPathTemplate.Contains("{companyId}", StringComparison.Ordinal)
            || !options.TrackingPathTemplate.Contains("{trackingCode}", StringComparison.Ordinal))
        {
            failures.Add("Loggi:TrackingPathTemplate must contain {companyId} and {trackingCode} placeholders.");
        }

        if (options.RequestTimeoutSeconds <= 0)
        {
            failures.Add("Loggi:RequestTimeoutSeconds must be greater than zero.");
        }

        if (options.TokenRefreshSkewMinutes < 0)
        {
            failures.Add("Loggi:TokenRefreshSkewMinutes cannot be negative.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
