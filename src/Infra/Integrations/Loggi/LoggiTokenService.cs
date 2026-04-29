using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Loggi;

public sealed class LoggiTokenService : ILoggiTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly LoggiOptions options;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private string? accessToken;
    private DateTimeOffset expiresAt;

    public LoggiTokenService(HttpClient httpClient, IOptions<LoggiOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (HasValidToken())
        {
            return accessToken!;
        }

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            if (HasValidToken())
            {
                return accessToken!;
            }

            var token = await RequestTokenAsync(cancellationToken);
            accessToken = token.AccessToken;
            expiresAt = token.ExpiresAt;

            return accessToken;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private bool HasValidToken()
    {
        return !string.IsNullOrWhiteSpace(accessToken)
            && expiresAt > DateTimeOffset.UtcNow.AddMinutes(options.TokenRefreshSkewMinutes);
    }

    private async Task<LoggiToken> RequestTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenPath);
        request.Content = JsonContent.Create(new
        {
            client_id = options.ClientId,
            client_secret = options.ClientSecret
        }, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response.StatusCode, $"Loggi returned HTTP {(int)response.StatusCode} while requesting a token.");
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return ParseToken(document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new LoggiApiException(LoggiApiErrorKind.InvalidResponse, $"Loggi returned invalid token JSON: {exception.Message}");
        }
    }

    private static LoggiToken ParseToken(JsonElement root)
    {
        var token = ReadString(root, "access_token")
            ?? ReadString(root, "accessToken")
            ?? ReadString(root, "token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new LoggiApiException(LoggiApiErrorKind.InvalidResponse, "Loggi token response did not include an access token.");
        }

        var expiresAt = ReadDateTime(root, "expires_at")
            ?? ReadDateTime(root, "expiresAt")
            ?? DateTimeOffset.UtcNow.AddSeconds(ReadInt(root, "expires_in") ?? ReadInt(root, "expiresIn") ?? 3600);

        return new LoggiToken(token, expiresAt);
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static DateTimeOffset? ReadDateTime(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(property.GetString(), out var value)
            ? value
            : null;
    }

    private static LoggiApiException ToException(HttpStatusCode statusCode, string message)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new LoggiApiException(LoggiApiErrorKind.Authentication, message),
            HttpStatusCode.Forbidden => new LoggiApiException(LoggiApiErrorKind.Authorization, message),
            HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError => new LoggiApiException(LoggiApiErrorKind.RateLimitOrTransient, message),
            _ => new LoggiApiException(LoggiApiErrorKind.Unknown, message)
        };
    }

    private sealed record LoggiToken(string AccessToken, DateTimeOffset ExpiresAt);
}
