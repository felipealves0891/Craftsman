using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Correios;

public sealed class CorreiosOptions
{
    public const string SectionName = "Correios";

    public bool Enabled { get; set; }

    public string Environment { get; set; } = "Production";

    public string BaseUrl { get; set; } = "https://api.correios.com.br";

    public string TokenBaseUrl { get; set; } = "https://api.correios.com.br";

    public string TrackingPathTemplate { get; set; } = "/sro-rastro/v1/objetos/{trackingCode}?resultado=T";

    public string UserName { get; set; } = string.Empty;

    public string ApiAccessCode { get; set; } = string.Empty;

    public string ContractNumber { get; set; } = string.Empty;

    public int? ContractDr { get; set; }

    public string PostCardNumber { get; set; } = string.Empty;

    public int RequestTimeoutSeconds { get; set; } = 30;

    public int TokenRefreshSkewMinutes { get; set; } = 5;
}

public sealed class CorreiosOptionsValidator : IValidateOptions<CorreiosOptions>
{
    public ValidateOptionsResult Validate(string? name, CorreiosOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("Correios:BaseUrl must be an absolute URL.");
        }

        if (string.IsNullOrWhiteSpace(options.TokenBaseUrl) || !Uri.TryCreate(options.TokenBaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("Correios:TokenBaseUrl must be an absolute URL.");
        }

        if (string.IsNullOrWhiteSpace(options.TrackingPathTemplate)
            || !options.TrackingPathTemplate.Contains("{trackingCode}", StringComparison.Ordinal))
        {
            failures.Add("Correios:TrackingPathTemplate must contain the {trackingCode} placeholder.");
        }

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            failures.Add("Correios:UserName must be configured when Correios is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiAccessCode))
        {
            failures.Add("Correios:ApiAccessCode must be configured when Correios is enabled.");
        }

        if (options.RequestTimeoutSeconds <= 0)
        {
            failures.Add("Correios:RequestTimeoutSeconds must be greater than zero.");
        }

        if (options.TokenRefreshSkewMinutes < 0)
        {
            failures.Add("Correios:TokenRefreshSkewMinutes cannot be negative.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
