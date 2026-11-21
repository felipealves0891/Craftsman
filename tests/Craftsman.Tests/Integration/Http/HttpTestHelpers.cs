using System.Net;
using System.Text.RegularExpressions;

namespace Craftsman.Tests.Integration.Http;

internal static partial class HttpTestHelpers
{
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, $"Nenhum token antiforgery encontrado em {path}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static FormUrlEncodedContent AntiforgeryForm(string token, params (string Name, string Value)[] fields)
    {
        var values = fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value)).ToList();
        values.Add(new("__RequestVerificationToken", token));
        return new FormUrlEncodedContent(values);
    }

    [GeneratedRegex("name=\\\"__RequestVerificationToken\\\"[^>]*value=\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
