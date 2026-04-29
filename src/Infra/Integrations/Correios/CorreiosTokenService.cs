using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Correios;

public sealed class CorreiosTokenService : ICorreiosTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly CorreiosOptions options;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private string? accessToken;
    private DateTimeOffset expiresAt;

    public CorreiosTokenService(HttpClient httpClient, IOptions<CorreiosOptions> options)
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

    private async Task<CorreiosToken> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var path = TokenPath();
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", BasicCredential());

        var body = TokenBody();
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response.StatusCode, $"Correios returned HTTP {(int)response.StatusCode} while requesting a token.");
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return ParseToken(document.RootElement);
        }
        catch (JsonException exception)
        {
            throw new CorreiosApiException(CorreiosApiErrorKind.InvalidResponse, $"Correios returned invalid token JSON: {exception.Message}");
        }
    }

    private string TokenPath()
    {
        if (!string.IsNullOrWhiteSpace(options.PostCardNumber))
        {
            return "/token/v1/autentica/cartaopostagem";
        }

        if (!string.IsNullOrWhiteSpace(options.ContractNumber))
        {
            return "/token/v1/autentica/contrato";
        }

        return "/token/v1/autentica";
    }

    private object? TokenBody()
    {
        if (!string.IsNullOrWhiteSpace(options.PostCardNumber))
        {
            return new
            {
                numero = options.PostCardNumber,
                contrato = string.IsNullOrWhiteSpace(options.ContractNumber) ? null : options.ContractNumber,
                dr = options.ContractDr
            };
        }

        if (!string.IsNullOrWhiteSpace(options.ContractNumber))
        {
            return new
            {
                numero = options.ContractNumber,
                dr = options.ContractDr
            };
        }

        return null;
    }

    private string BasicCredential()
    {
        var raw = $"{options.UserName}:{options.ApiAccessCode}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static CorreiosToken ParseToken(JsonElement root)
    {
        var token = ReadString(root, "token")
            ?? ReadString(root, "access_token")
            ?? ReadString(root, "accessToken")
            ?? ReadString(root, "bearerToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new CorreiosApiException(CorreiosApiErrorKind.InvalidResponse, "Correios token response did not include a token.");
        }

        var expiresAt = ReadDateTime(root, "expiraEm")
            ?? ReadDateTime(root, "expires_at")
            ?? ReadDateTime(root, "expiresAt")
            ?? DateTimeOffset.UtcNow.AddSeconds(ReadInt(root, "expires_in") ?? ReadInt(root, "expiresIn") ?? 3600);

        return new CorreiosToken(token, expiresAt);
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

    private static CorreiosApiException ToException(HttpStatusCode statusCode, string message)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new CorreiosApiException(CorreiosApiErrorKind.Authentication, message),
            HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError => new CorreiosApiException(CorreiosApiErrorKind.RateLimitOrTransient, message),
            _ => new CorreiosApiException(CorreiosApiErrorKind.Unknown, message)
        };
    }

    private sealed record CorreiosToken(string AccessToken, DateTimeOffset ExpiresAt);
}
