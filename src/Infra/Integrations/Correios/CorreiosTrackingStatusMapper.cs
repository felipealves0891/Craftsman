using Craftsman.Domain.Shipping.Entities;
using System.Globalization;
using System.Text;

namespace Craftsman.Infra.Integrations.Correios;

public static class CorreiosTrackingStatusMapper
{
    public static ShipmentStatus ToShipmentStatus(CorreiosTrackingEvent trackingEvent)
    {
        var text = Normalize($"{trackingEvent.Code} {trackingEvent.Type} {trackingEvent.Description}");

        if (ContainsAny(text, "entregue", "objeto entregue"))
        {
            return ShipmentStatus.Delivered;
        }

        if (ContainsAny(text, "tentativa", "destinatario ausente", "aguardando retirada", "nao efetuada"))
        {
            return ShipmentStatus.DeliveryAttempted;
        }

        if (ContainsAny(text, "postado", "encaminhado", "transito", "saiu para entrega", "fiscalizacao"))
        {
            return ShipmentStatus.InTransit;
        }

        return ShipmentStatus.Created;
    }

    private static bool ContainsAny(string value, params string[] patterns)
    {
        return patterns.Any(pattern => value.Contains(Normalize(pattern), StringComparison.Ordinal));
    }

    private static string Normalize(string value)
    {
        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
