using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Craftsman.Infra.Integrations.Loggi;

public sealed class LoggiTrackingClient : ILoggiTrackingClient
{
    private readonly HttpClient httpClient;
    private readonly ILoggiTokenService tokenService;
    private readonly LoggiOptions options;
    private readonly ILogger<LoggiTrackingClient> logger;

    public LoggiTrackingClient(
        HttpClient httpClient,
        ILoggiTokenService tokenService,
        IOptions<LoggiOptions> options,
        ILogger<LoggiTrackingClient> logger)
    {
        this.httpClient = httpClient;
        this.tokenService = tokenService;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<LoggiTrackingEvent?> GetLatestEventAsync(string trackingCode, CancellationToken cancellationToken = default)
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
            throw ToException(response.StatusCode, $"Loggi returned HTTP {(int)response.StatusCode} while tracking a package.");
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return ParseLatestEvent(document.RootElement, normalizedTrackingCode);
        }
        catch (JsonException exception)
        {
            throw new LoggiApiException(LoggiApiErrorKind.InvalidResponse, $"Loggi returned invalid tracking JSON: {exception.Message}");
        }
    }

    private Uri BuildTrackingUri(string trackingCode)
    {
        var relative = options.TrackingPathTemplate
            .Replace("{companyId}", Uri.EscapeDataString(options.CompanyId), StringComparison.Ordinal)
            .Replace("{trackingCode}", Uri.EscapeDataString(trackingCode), StringComparison.Ordinal);

        return new Uri(httpClient.BaseAddress!, relative);
    }

    private LoggiTrackingEvent? ParseLatestEvent(JsonElement root, string fallbackTrackingCode)
    {
        var trackingCode = ReadString(root, "trackingCode")
            ?? ReadString(root, "tracking_code")
            ?? fallbackTrackingCode;

        var latestEvent = EventElements(root)
            .Select(element => ParseEvent(element, trackingCode))
            .Where(trackingEvent => trackingEvent is not null)
            .OrderByDescending(trackingEvent => trackingEvent!.UpdatedAt ?? DateTimeOffset.MinValue)
            .FirstOrDefault();

        if (latestEvent is not null)
        {
            return latestEvent;
        }

        var currentStatus = StatusElement(root);
        if (currentStatus is not null)
        {
            return ParseEvent(currentStatus.Value, trackingCode);
        }

        logger.LogInformation("Loggi tracking response did not contain a status for {TrackingCode}.", fallbackTrackingCode);
        return null;
    }

    private static LoggiTrackingEvent? ParseEvent(JsonElement element, string trackingCode)
    {
        var statusElement = StatusElement(element) ?? element;
        var statusCode = ReadInt(statusElement, "code")
            ?? ReadInt(statusElement, "status")
            ?? ReadInt(statusElement, "statusCode")
            ?? ReadInt(statusElement, "status_code");

        if (statusCode is null)
        {
            return null;
        }

        return new LoggiTrackingEvent(
            trackingCode,
            statusCode,
            ReadString(statusElement, "description")
                ?? ReadString(statusElement, "label")
                ?? ReadString(statusElement, "name")
                ?? ReadString(element, "description"),
            ReadDateTime(statusElement, "updatedTime")
                ?? ReadDateTime(statusElement, "updated_time")
                ?? ReadDateTime(element, "updatedTime")
                ?? ReadDateTime(element, "updated_time")
                ?? ReadDateTime(element, "createdTime")
                ?? ReadDateTime(element, "created_time"));
    }

    private static IEnumerable<JsonElement> EventElements(JsonElement root)
    {
        if (root.TryGetProperty("history", out var historyElement) && historyElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var eventElement in historyElement.EnumerateArray())
            {
                yield return eventElement;
            }
        }

        if (root.TryGetProperty("events", out var eventsElement) && eventsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var eventElement in eventsElement.EnumerateArray())
            {
                yield return eventElement;
            }
        }

        if (root.TryGetProperty("tracking", out var trackingElement)
            && trackingElement.ValueKind == JsonValueKind.Object
            && trackingElement.TryGetProperty("history", out var trackingHistoryElement)
            && trackingHistoryElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var eventElement in trackingHistoryElement.EnumerateArray())
            {
                yield return eventElement;
            }
        }
    }

    private static JsonElement? StatusElement(JsonElement element)
    {
        if (element.TryGetProperty("status", out var statusElement) && statusElement.ValueKind == JsonValueKind.Object)
        {
            return statusElement;
        }

        if (element.TryGetProperty("currentStatus", out var currentStatusElement) && currentStatusElement.ValueKind == JsonValueKind.Object)
        {
            return currentStatusElement;
        }

        if (element.TryGetProperty("current_status", out var currentStatusSnakeElement) && currentStatusSnakeElement.ValueKind == JsonValueKind.Object)
        {
            return currentStatusSnakeElement;
        }

        return null;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        if (property.TryGetInt32(out var value))
        {
            return value;
        }

        return property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value)
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
}
