using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging();
builder.Services.Configure<VulavulaSettings>(builder.Configuration.GetSection("Vulavula"));
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/energy/dashboard", () => Results.Ok(EnergyDemo.CreateSnapshot()));
app.MapGet("/api/energy/history", () => Results.Ok(EnergyDemo.CreateHistory()));
app.MapPost("/api/energy/scan", () => Results.Ok(EnergyDemo.CreateSnapshot(true)));
app.MapPost("/api/energy/heartbeat", (EnergyHeartbeat heartbeat) =>
{
    if (string.IsNullOrWhiteSpace(heartbeat.ComputerId))
        return Results.BadRequest(new { error = "ComputerId is required." });
    return Results.Ok(EnergyDemo.FromHeartbeat(heartbeat));
});

app.MapPost("/api/translate", async Task<IResult> (
    TranslateProxyRequest req,
    IOptions<VulavulaSettings> settings,
    IHttpClientFactory httpClientFactory,
    ILogger<Program> logger) =>
{
    if (string.IsNullOrWhiteSpace(req.Text))
        return Results.BadRequest(new { ok = false, error = "Text is required." });
    if (req.Text.Length > 5000)
        return Results.BadRequest(new { ok = false, error = "Text must be 5000 characters or less." });

    var opts = settings.Value;
    var apiKey = opts.ApiKey;
    var endpoint = opts.Endpoint;
    var source = NormalizeLanguage(req.SourceLanguage, opts.DefaultSourceLanguage);
    var target = NormalizeLanguage(req.TargetLanguage, opts.DefaultTargetLanguage);

    if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(endpoint))
    {
        var mock = GetMockTranslation(req.Text, target);
        return Results.Ok(DemoResponse(mock, req.Text, source, target));
    }

    try
    {
        var payload = new { text = req.Text, source_lang = source, target_lang = target };
        using var client = httpClientFactory.CreateClient();
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        };
        message.Headers.TryAddWithoutValidation("X-CLIENT-TOKEN", apiKey);

        var sw = Stopwatch.StartNew();
        using var response = await client.SendAsync(message);
        sw.Stop();
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Vulavula returned {StatusCode}: {Body}", response.StatusCode, responseText);
            return Results.Ok(new { ok = false, status = (int)response.StatusCode, responseTimeMs = sw.ElapsedMilliseconds, error = "Vulavula request failed.", details = responseText });
        }

        using var document = JsonDocument.Parse(responseText);
        var translated = ExtractTranslation(document.RootElement);
        if (string.IsNullOrWhiteSpace(translated))
            return Results.Ok(new { ok = false, status = 200, error = "No translation text was returned by Vulavula.", rawText = responseText });

        return Results.Ok(new
        {
            ok = true,
            mode = "vulavula",
            status = (int)response.StatusCode,
            responseTimeMs = sw.ElapsedMilliseconds,
            sourceLanguage = source,
            targetLanguage = target,
            translatedTexts = new[] { translated },
            rawBody = document.RootElement
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Translation request failed.");
        return Results.Ok(new { ok = false, status = 500, error = "Translation service could not be reached.", details = ex.Message });
    }
});

app.Run();

static string NormalizeLanguage(string language, string fallback)
{
    var value = string.IsNullOrWhiteSpace(language) ? fallback : language;
    return value switch
    {
        "en" => "eng_Latn",
        "zu" => "zul_Latn",
        "xh" => "xho_Latn",
        "af" => "afr_Latn",
        "st" => "sot_Latn",
        "tn" => "tsn_Latn",
        "nso" => "nso_Latn",
        "ss" => "ssw_Latn",
        "ve" => "ven_Latn",
        "ts" => "tso_Latn",
        "nr" => "nbl_Latn",
        _ => value
    };
}

static string? ExtractTranslation(JsonElement root)
{
    if (root.ValueKind == JsonValueKind.String) return root.GetString();
    if (root.TryGetProperty("translation", out var translation))
    {
        if (translation.ValueKind == JsonValueKind.String) return translation.GetString();
        if (translation.ValueKind == JsonValueKind.Array && translation.GetArrayLength() > 0)
        {
            var first = translation[0];
            if (first.TryGetProperty("translation_text", out var text)) return text.GetString();
            if (first.TryGetProperty("text", out text)) return text.GetString();
        }
    }
    if (root.TryGetProperty("translations", out var translations) && translations.ValueKind == JsonValueKind.Array && translations.GetArrayLength() > 0)
    {
        var first = translations[0];
        if (first.ValueKind == JsonValueKind.String) return first.GetString();
        if (first.TryGetProperty("translation_text", out var text)) return text.GetString();
        if (first.TryGetProperty("text", out text)) return text.GetString();
    }
    if (root.TryGetProperty("__value__", out var value) && value.ValueKind == JsonValueKind.String) return value.GetString();
    return null;
}

static object DemoResponse(string translation, string original, string source, string target) => new
{
    ok = true,
    mode = "demo",
    status = 200,
    responseTimeMs = 8,
    sourceLanguage = source,
    targetLanguage = target,
    translatedTexts = new[] { translation },
    rawBody = new { demo = true, original }
};

static string GetMockTranslation(string text, string targetLang)
{
    if (!targetLang.Contains("zul", StringComparison.OrdinalIgnoreCase)) return $"[demo] {text}";
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["hello"] = "Sawubona", ["goodbye"] = "Hamba kahle", ["thank you"] = "Ngiyabonga",
        ["yes"] = "Yebo", ["no"] = "Cha", ["computer"] = "ikhompyutha", ["computers"] = "amakhompyutha",
        ["lab"] = "ilebhu", ["energy"] = "amandla", ["waste"] = "ukumosa", ["electricity"] = "ugesi",
        ["idle"] = "ayisebenzi", ["running"] = "iyasebenza", ["close"] = "vala", ["delete"] = "sula",
        ["carbon"] = "ikhabhoni", ["footprint"] = "umkhondo", ["now"] = "manje"
    };
    if (dict.TryGetValue(text.Trim(), out var exact)) return exact;
    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => dict.TryGetValue(word.Trim('.', ',', '!', '?'), out var translated) ? translated : word);
    return string.Join(' ', words);
}

public static class EnergyDemo
{
    private static readonly string[] Rooms = { "Lab A", "Lab A", "Lab B", "Lab B", "Lab C", "Lab C", "Lab D", "Lab D" };
    private static readonly Random Random = new();

    public static EnergyDashboard CreateSnapshot(bool randomize = false)
    {
        var computers = Enumerable.Range(1, 20).Select(i =>
        {
            var idle = randomize ? Random.NextDouble() < .35 : i % 5 == 0;
            var overnight = randomize ? Random.NextDouble() < .20 : i == 14;
            var cpu = idle ? Random.Next(2, 14) : Random.Next(18, 78);
            var watts = idle ? Random.Next(18, 42) : Random.Next(55, 135);
            var hours = overnight ? Random.Next(8, 13) : Random.Next(0, 6);
            var kwh = Math.Round(watts * hours / 1000.0, 2);
            return new EnergyComputer($"Computer {i:00}", Rooms[(i - 1) % Rooms.Length], cpu, watts, hours, kwh,
                idle || overnight ? "Waste detected" : "Normal", idle ? "Idle" : overnight ? "Overnight run" : "Active");
        }).ToList();

        var waste = computers.Where(c => c.Status == "Waste detected").ToList();
        var kwhTotal = computers.Sum(c => c.EstimatedKwhToday);
        var carbon = Math.Round(kwhTotal * .708, 2);
        return new EnergyDashboard(DateTimeOffset.Now, computers.Count, waste.Count, Math.Round(kwhTotal, 2), carbon,
            Math.Round(waste.Sum(c => c.EstimatedKwhToday), 2), computers,
            waste.Take(5).Select(c => new EnergyAlert(c.ComputerId,
                $"{c.ComputerId} is {c.Condition.ToLowerInvariant()}. Estimated waste: {c.EstimatedKwhToday:0.00} kWh today.", "High")).ToList());
    }

    public static List<EnergyHistoryPoint> CreateHistory()
    {
        var today = DateTime.Today;
        var values = new (double kwh, int alerts)[]
        {
            (18.7, 7), (20.1, 9), (17.9, 6), (22.4, 11), (21.2, 10), (19.3, 8), (23.6, 12)
        };
        return values.Select((v, i) =>
        {
            var date = today.AddDays(i - (values.Length - 1));
            var carbon = Math.Round(v.kwh * .708, 2);
            return new EnergyHistoryPoint(date, Math.Round(v.kwh, 2), v.alerts, carbon, Math.Round(v.alerts * .62, 2));
        }).ToList();
    }

    public static EnergyComputer FromHeartbeat(EnergyHeartbeat h)
    {
        var kwh = Math.Round(Math.Max(0, h.Watts) * Math.Max(0, h.HoursObserved) / 1000.0, 3);
        var waste = h.Idle || h.HoursObserved >= 8;
        return new EnergyComputer(h.ComputerId, h.Room ?? "Unassigned", h.CpuPercent, h.Watts, h.HoursObserved, kwh,
            waste ? "Waste detected" : "Normal", h.Idle ? "Idle" : h.HoursObserved >= 8 ? "Overnight run" : "Active");
    }
}

public record VulavulaSettings
{
    public string Endpoint { get; init; } = "https://vulavula-services.lelapa.ai/api/v1/translate";
    public string ApiKey { get; init; } = string.Empty;
    public string DefaultSourceLanguage { get; init; } = "eng_Latn";
    public string DefaultTargetLanguage { get; init; } = "zul_Latn";
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
public record EnergyHeartbeat(string ComputerId, string? Room, double CpuPercent, double Watts, double HoursObserved, bool Idle);
public record EnergyDashboard(DateTimeOffset Timestamp, int ComputerCount, int WasteAlerts, double EstimatedKwhToday, double EstimatedCarbonKg, double EstimatedWasteKwh, List<EnergyComputer> Computers, List<EnergyAlert> Alerts);
public record EnergyComputer(string ComputerId, string Room, double CpuPercent, double Watts, double HoursObserved, double EstimatedKwhToday, string Status, string Condition);
public record EnergyAlert(string ComputerId, string Message, string Severity);
public record EnergyHistoryPoint(DateTime Date, double Kwh, int Alerts, double CarbonKg, double WasteKwh);
