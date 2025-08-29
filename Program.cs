using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AzureTranslatorOptions>(builder.Configuration.GetSection("AzureTranslator"));
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/translate", async Task<Results<BadRequest<string>, JsonHttpResult<object>, ProblemHttpResult>> (
    TranslateProxyRequest req,
    IOptions<AzureTranslatorOptions> options,
    IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(req.Text))
    {
        return TypedResults.BadRequest("Text is required.");
    }
    if (req.Text.Length > 5000)
    {
        return TypedResults.BadRequest("Text must be 5000 characters or less.");
    }
    if (string.IsNullOrWhiteSpace(req.TargetLanguage))
    {
        return TypedResults.BadRequest("TargetLanguage is required.");
    }

    var opts = options.Value;
    if (string.IsNullOrWhiteSpace(opts.Endpoint) || string.IsNullOrWhiteSpace(opts.Key) || string.IsNullOrWhiteSpace(opts.Region))
    {
        return TypedResults.Problem("Azure Translator settings are not configured. Set AzureTranslator:Endpoint, Key and Region in appsettings.json or environment variables.", statusCode: 500);
    }

    var payload = new object[]
    {
        new {
            Text = req.Text,
            language = string.IsNullOrWhiteSpace(req.SourceLanguage) ? null : req.SourceLanguage,
            targets = new object[] {
                new {
                    language = req.TargetLanguage,
                    deploymentName = string.IsNullOrWhiteSpace(req.DeploymentName) ? null : req.DeploymentName,
                    gender = string.IsNullOrWhiteSpace(req.Gender) ? null : req.Gender,
                    tone = string.IsNullOrWhiteSpace(req.Tone) ? null : req.Tone
                }
            }
        }
    };

    var path = string.IsNullOrWhiteSpace(opts.Path) ? "translator/text/translate" : opts.Path.TrimStart('/');
    var url = $"{opts.Endpoint.TrimEnd('/')}/{path}?api-version={opts.ApiVersion ?? "2025-05-01-preview"}";

    using var client = httpClientFactory.CreateClient();
    using var message = new HttpRequestMessage(HttpMethod.Post, url)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }), System.Text.Encoding.UTF8, "application/json")
    };

    message.Headers.Add("Ocp-Apim-Subscription-Key", opts.Key);
    message.Headers.Add("Ocp-Apim-Subscription-Region", opts.Region);
    message.Headers.Add("preview-api", "true");

    var sw = Stopwatch.StartNew();
    using var response = await client.SendAsync(message);
    sw.Stop();

    var responseText = await response.Content.ReadAsStringAsync();

    JsonElement? body = null;
    try
    {
        body = JsonSerializer.Deserialize<JsonElement>(responseText);
    }
    catch
    {
        // leave body null
    }

    var translatedTexts = new List<string>();
    if (body.HasValue)
    {
        ExtractTextNodes(body.Value, translatedTexts);
    }

    var headers = response.Headers;
    var contentHeaders = response.Content.Headers;

    string? xRequestId = GetFirstResponseHeader(headers, "x-requestid");
    // Use only the new headers exposed by the service
    string? sourceCharsHeader = GetFirstResponseHeader(headers, "sourcecharacterscharged")
        ?? GetFirstContentHeader(contentHeaders, "sourcecharacterscharged");
    string? sourceTokensHeader = GetFirstResponseHeader(headers, "sourcetokenscharged")
        ?? GetFirstContentHeader(contentHeaders, "sourcetokenscharged");
    string? targetTokensHeader = GetFirstResponseHeader(headers, "targettokenscharged")
        ?? GetFirstContentHeader(contentHeaders, "targettokenscharged");

    int? charactersCharged = TryParseFirstInt(sourceCharsHeader);
    int? sourceTokens = TryParseFirstInt(sourceTokensHeader);
    int? targetTokens = TryParseFirstInt(targetTokensHeader);

    var result = new
    {
        ok = response.IsSuccessStatusCode,
        status = (int)response.StatusCode,
        responseTimeMs = sw.ElapsedMilliseconds,
        headers = new
        {
            charactersCharged,
            sourceTokensCharged = sourceTokens,
            targetTokensCharged = targetTokens,
            xRequestId = xRequestId
        },
        translatedTexts,
        rawBody = body,
        rawText = responseText
    };

    return TypedResults.Json((object)result);
});

app.MapGet("/api/languages", async (IHttpClientFactory httpClientFactory) =>
{
    var url = "https://api.cognitive.microsofttranslator.com/languages?api-version=2025-05-01-preview&scope=translation";
    using var client = httpClientFactory.CreateClient();
    using var response = await client.GetAsync(url);
    var text = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem($"Languages fetch failed: {(int)response.StatusCode}", statusCode: (int)response.StatusCode);
    }

    JsonDocument doc;
    try { doc = JsonDocument.Parse(text); }
    catch
    {
        return Results.Problem("Failed to parse languages JSON.", statusCode: 500);
    }

    var root = doc.RootElement;
    if (!root.TryGetProperty("translation", out var translationNode) || translationNode.ValueKind != JsonValueKind.Object)
    {
        return Results.Json(new { raw = root });
    }

    var list = new List<LanguageItem>();
    foreach (var langProp in translationNode.EnumerateObject())
    {
        var code = langProp.Name;
        var value = langProp.Value;
        string? name = null;
        string? nativeName = null;
        string? dir = null;
        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String) name = n.GetString();
            if (value.TryGetProperty("nativeName", out var nn) && nn.ValueKind == JsonValueKind.String) nativeName = nn.GetString();
            if (value.TryGetProperty("dir", out var d) && d.ValueKind == JsonValueKind.String) dir = d.GetString();
        }
        list.Add(new LanguageItem(code, name, nativeName, dir));
    }

    list.Sort((a, b) => string.Compare(a.name ?? a.code, b.name ?? b.code, StringComparison.OrdinalIgnoreCase));

    return Results.Json(new { languages = list });
});

app.Run();

static string? GetFirstResponseHeader(System.Net.Http.Headers.HttpResponseHeaders headers, string name)
{
    if (headers.TryGetValues(name, out var values))
    {
        return values.FirstOrDefault();
    }
    return null;
}

static string? GetFirstContentHeader(System.Net.Http.Headers.HttpContentHeaders headers, string name)
{
    if (headers.TryGetValues(name, out var values))
    {
        return values.FirstOrDefault();
    }
    return null;
}

static int? TryParseFirstInt(string? input)
{
    if (string.IsNullOrWhiteSpace(input)) return null;
    var num = new string(input!.Where(char.IsDigit).ToArray());
    if (int.TryParse(num, out var value)) return value;
    return null;
}


static void ExtractTextNodes(JsonElement element, List<string> sink)
{
    switch (element.ValueKind)
    {
        case JsonValueKind.Object:
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.NameEquals("text") && prop.Value.ValueKind == JsonValueKind.String)
                {
                    sink.Add(prop.Value.GetString() ?? string.Empty);
                }
                ExtractTextNodes(prop.Value, sink);
            }
            break;
        case JsonValueKind.Array:
            foreach (var item in element.EnumerateArray())
            {
                ExtractTextNodes(item, sink);
            }
            break;
        default:
            break;
    }
}

record TranslateProxyRequest
{
    public string Text { get; init; } = string.Empty;
    public string SourceLanguage { get; init; } = string.Empty; // e.g., "en"
    public string TargetLanguage { get; init; } = string.Empty; // e.g., "es"
    public string? DeploymentName { get; init; }
    public string? Gender { get; init; } // "female" or "male"
    public string? Tone { get; init; } // "formal", "informal", etc.
}

class AzureTranslatorOptions
{
    public string Endpoint { get; set; } = string.Empty; // https://<your-resource-name>.cognitiveservices.azure.com
    public string Key { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string? ApiVersion { get; set; } = "2025-05-01-preview";
    public string? Path { get; set; } = "translator/text/translate";
}

record LanguageItem(string code, string? name, string? nativeName, string? dir);
