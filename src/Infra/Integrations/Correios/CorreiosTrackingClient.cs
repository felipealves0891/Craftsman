using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Correios;

public sealed class CorreiosTrackingClient : ICorreiosTrackingClient
{
    private readonly HttpClient httpClient;
    private readonly ICorreiosTokenService tokenService;
    private readonly CorreiosOptions options;
    private readonly ILogger<CorreiosTrackingClient> logger;

    public CorreiosTrackingClient(
        HttpClient httpClient,
        ICorreiosTokenService tokenService,
        IOptions<CorreiosOptions> options,
        ILogger<CorreiosTrackingClient> logger)
    {
        this.httpClient = httpClient;
        this.tokenService = tokenService;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<CorreiosTrackingEvent?> GetLatestEventAsync(string trackingCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(trackingCode))
        {
            return null;
        }

        var normalizedTrackingCode = trackingCode.Trim();
        var accessToken = await tokenService.GetAccessTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildTrackingUri(normalizedTrackingCode));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw ToException(response.StatusCode, $"Correios returned HTTP {(int)response.StatusCode} while tracking an object.");
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return ParseLatestEvent(document.RootElement, normalizedTrackingCode);
        }
        catch (JsonException exception)
        {
            throw new CorreiosApiException(CorreiosApiErrorKind.InvalidResponse, $"Correios returned invalid tracking JSON: {exception.Message}");
        }
    }

    private Uri BuildTrackingUri(string trackingCode)
    {
        var escapedTrackingCode = Uri.EscapeDataString(trackingCode);
        var relative = options.TrackingPathTemplate.Replace("{trackingCode}", escapedTrackingCode, StringComparison.Ordinal);

        return new Uri(httpClient.BaseAddress!, relative);
    }

    private CorreiosTrackingEvent? ParseLatestEvent(JsonElement root, string fallbackTrackingCode)
    {
        var objectElement = FirstObject(root);
        if (objectElement is null)
        {
            logger.LogInformation("Correios tracking response did not contain an object for {TrackingCode}.", fallbackTrackingCode);
            return null;
        }

        var trackingCode = ReadString(objectElement.Value, "codObjeto")
            ?? ReadString(objectElement.Value, "codigoObjeto")
            ?? fallbackTrackingCode;

        if (!objectElement.Value.TryGetProperty("eventos", out var eventsElement) || eventsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        CorreiosTrackingEvent? latestEvent = null;
        foreach (var eventElement in eventsElement.EnumerateArray())
        {
            var currentEvent = new CorreiosTrackingEvent(
                trackingCode,
                ReadString(eventElement, "codigo"),
                ReadString(eventElement, "tipo"),
                ReadString(eventElement, "descricao") ?? ReadString(eventElement, "detalhe"),
                ReadDateTime(eventElement, "dtHrCriado") ?? ReadDateTime(eventElement, "dataHora") ?? ReadDateTime(eventElement, "criadoEm"));

            if (latestEvent is null || (currentEvent.CreatedAt ?? DateTimeOffset.MinValue) > (latestEvent.CreatedAt ?? DateTimeOffset.MinValue))
            {
                latestEvent = currentEvent;
            }
        }

        return latestEvent;
    }

    private static JsonElement? FirstObject(JsonElement root)
    {
        if (root.TryGetProperty("objetos", out var objectsElement) && objectsElement.ValueKind == JsonValueKind.Array)
        {
            return objectsElement.EnumerateArray().FirstOrDefault();
        }

        if (root.TryGetProperty("objeto", out var objectElement) && objectElement.ValueKind == JsonValueKind.Object)
        {
            return objectElement;
        }

        return root.ValueKind == JsonValueKind.Object ? root : null;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
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
}
