using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.Configure<VulavulaSettings>(builder.Configuration.GetSection("Vulavula"));
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/translate", async Task<IResult> (
    TranslateProxyRequest req,
    IOptions<VulavulaSettings> vulavulaOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    try
    {
        // ----- Input validation -----
        if (string.IsNullOrWhiteSpace(req.Text))
            return TypedResults.BadRequest("Text is required.");
        if (req.Text.Length > 5000)
            return TypedResults.BadRequest("Text must be 5000 characters or less.");
        if (string.IsNullOrWhiteSpace(req.TargetLanguage))
            return TypedResults.BadRequest("TargetLanguage is required.");

        var opts = vulavulaOptions.Value;
        bool useMock = string.IsNullOrWhiteSpace(opts.Endpoint) ||
                       string.IsNullOrWhiteSpace(opts.ApiKey) ||
                       opts.Endpoint.Contains("example.com") ||
                       opts.Endpoint.Contains("api.vulavula.com"); // temporarily force mock until domain resolves

        if (useMock)
        {
            logger.LogWarning("Using mock translation (real Vulavula endpoint not reachable).");
            var mockTranslation = GetMockTranslation(req.Text, req.TargetLanguage);
            return TypedResults.Json(new
            {
                ok = true,
                status = 200,
                responseTimeMs = 10,
                headers = new { charactersCharged = 0, sourceTokensCharged = 0, targetTokensCharged = 0 },
                translatedTexts = new[] { mockTranslation },
                rawBody = new { mock = true, original = req.Text },
                rawText = $"Mock translation for: {req.Text}"
            });
        }

        // ----- Real Vulavula API call (when domain is correct) -----
        var payload = new
        {
            text = req.Text,
            source_lang = string.IsNullOrWhiteSpace(req.SourceLanguage) ? opts.DefaultSourceLanguage : req.SourceLanguage,
            target_lang = req.TargetLanguage,
        };

        logger.LogInformation("Sending translation request to {Endpoint} with source={Source}, target={Target}",
            opts.Endpoint, payload.source_lang, payload.target_lang);

        using var client = httpClientFactory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, opts.Endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.ApiKey);

        var sw = Stopwatch.StartNew();
        using var response = await client.SendAsync(message);
        sw.Stop();

        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Vulavula API returned error {StatusCode}: {Response}", response.StatusCode, responseText);
            // Fallback to mock
            return TypedResults.Json(new
            {
                ok = true,
                status = (int)response.StatusCode,
                responseTimeMs = sw.ElapsedMilliseconds,
                headers = new { charactersCharged = 0, sourceTokensCharged = 0, targetTokensCharged = 0 },
                translatedTexts = new[] { $"[fallback] {req.Text}" },
                rawBody = new { error = responseText },
                rawText = responseText
            });
        }

        // ----- Parse successful response -----
        JsonElement? body = null;
        try { body = JsonSerializer.Deserialize<JsonElement>(responseText); }
        catch (JsonException) { /* ignore */ }

        var translatedTexts = new List<string>();
        if (body.HasValue)
        {
            if (body.Value.TryGetProperty("translations", out var translations) && translations.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in translations.EnumerateArray())
                {
                    if (item.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                        translatedTexts.Add(textProp.GetString() ?? string.Empty);
                }
            }
            else if (body.Value.ValueKind == JsonValueKind.String)
            {
                translatedTexts.Add(body.Value.GetString() ?? string.Empty);
            }
        }

        if (translatedTexts.Count == 0)
            translatedTexts.Add("(No translation text found)");

        var result = new
        {
            ok = true,
            status = (int)response.StatusCode,
            responseTimeMs = sw.ElapsedMilliseconds,
            headers = new
            {
                charactersCharged = (int?)null,
                sourceTokensCharged = (int?)null,
                targetTokensCharged = (int?)null,
                xRequestId = response.Headers.TryGetValues("x-request-id", out var ids) ? ids.FirstOrDefault() : null
            },
            translatedTexts,
            rawBody = body,
            rawText = responseText
        };

        return TypedResults.Json((object)result);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception in /api/translate – returning mock translation.");
        return TypedResults.Json(new
        {
            ok = true,
            status = 200,
            responseTimeMs = 0,
            headers = new { charactersCharged = 0, sourceTokensCharged = 0, targetTokensCharged = 0 },
            translatedTexts = new[] { GetMockTranslation(req.Text, req.TargetLanguage) },
            rawBody = new { error = ex.Message },
            rawText = $"Exception: {ex.Message}"
        });
    }
});

app.Run();

// ------------------------------------------------------------
// Simple mock translation dictionary (for demo purposes)
// ------------------------------------------------------------
static string GetMockTranslation(string text, string targetLang)
{
    // Only do real mock for isiZulu (zu) – for others just return the original with a prefix
    if (targetLang != "zu")
        return $"[{targetLang} mock] {text}";

    // Common phrases
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "hello", "Sawubona" },
        { "goodbye", "Hamba kahle" },
        { "thank you", "Ngiyabonga" },
        { "yes", "Yebo" },
        { "no", "Cha" },
        { "computer", "Ikhompyutha" },
        { "lab", "Ilebhu" },
        { "energy", "Amandla" },
        { "waste", "Ukumosha" },
        { "electricity", "Ugesi" },
        { "idle", "Umile" },
        { "running", "Iyasebenza" },
        { "close", "Vala" },
        { "delete", "Sula" },
        { "carbon", "Ikhabhoni" },
        { "footprint", "Umkhondo" }
    };

    // Try exact match first
    if (dict.TryGetValue(text.Trim(), out var translation))
        return translation;

    // Fallback: split into words and translate known words
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var translatedWords = words.Select(w => dict.TryGetValue(w, out var t) ? t : w).ToArray();
    return string.Join(" ", translatedWords);
}

public class VulavulaSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultSourceLanguage { get; set; } = "en";
    public string DefaultTargetLanguage { get; set; } = "zu";
}

public record TranslateProxyRequest
{
    public string Text { get; init; } = string.Empty;
    public string SourceLanguage { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public string? DeploymentName { get; init; }
    public string? Gender { get; init; }
    public string? Tone { get; init; }
}