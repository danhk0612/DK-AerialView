using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DKAerialView.Services;

public sealed record OpenRouterImageModel(
    string Id,
    string Name,
    int MaxInputReferences,
    IReadOnlyList<string> Resolutions,
    IReadOnlyList<string> AspectRatios)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Id : $"{Name}  ({Id})";
    public string CapabilitySummary
    {
        get
        {
            var resolutionText = Resolutions.Count > 0 ? string.Join(", ", Resolutions) : "모델 기본값";
            var ratioText = AspectRatios.Count > 0 ? string.Join(", ", AspectRatios) : "모델 기본값";
            return $"참조 이미지 최대 {MaxInputReferences}장 · 해상도 {resolutionText} · 비율 {ratioText}";
        }
    }
}

public sealed class OpenRouterImageService
{
    private readonly HttpClient _httpClient;

    public OpenRouterImageService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
    }

    public async Task<IReadOnlyList<OpenRouterImageModel>> GetEditingModelsAsync(
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/images/models");
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenRouter {(int)response.StatusCode}: {TryReadErrorMessage(responseText)}");

        using var doc = JsonDocument.Parse(responseText);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            return Array.Empty<OpenRouterImageModel>();

        var models = new List<OpenRouterImageModel>();
        foreach (var item in data.EnumerateArray())
        {
            if (!TryReadString(item, "id", out var id)) continue;
            var name = TryReadString(item, "name", out var parsedName) ? parsedName : id;

            if (!item.TryGetProperty("architecture", out var architecture) ||
                !architecture.TryGetProperty("input_modalities", out var inputModalities) ||
                !ContainsString(inputModalities, "image"))
                continue;

            if (!item.TryGetProperty("supported_parameters", out var supportedParameters) ||
                !supportedParameters.TryGetProperty("input_references", out var inputReferences))
                continue;

            var maxInputReferences = inputReferences.TryGetProperty("max", out var maxNode) && maxNode.TryGetInt32(out var max) ? max : 0;
            if (maxInputReferences < 1) continue;

            var resolutions = ReadEnumValues(supportedParameters, "resolution");
            var aspectRatios = ReadEnumValues(supportedParameters, "aspect_ratio");
            models.Add(new OpenRouterImageModel(id, name, maxInputReferences, resolutions, aspectRatios));
        }

        return models.OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<byte[]> EnhanceAsync(
        string apiKey,
        string model,
        byte[] sourceImage,
        string prompt,
        string resolution,
        string aspectRatio,
        CancellationToken cancellationToken = default)
    {
        var dataUrl = $"data:image/png;base64,{Convert.ToBase64String(sourceImage)}";
        var request = new
        {
            model,
            prompt,
            resolution,
            aspect_ratio = aspectRatio,
            output_format = "png",
            input_references = new[]
            {
                new
                {
                    type = "image_url",
                    image_url = new { url = dataUrl }
                }
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/images")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenRouter {(int)response.StatusCode}: {TryReadErrorMessage(responseText)}");

        var body = JsonSerializer.Deserialize<ImageResponse>(responseText);
        var base64 = body?.Data?.FirstOrDefault()?.Base64;
        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("OpenRouter 응답에 이미지 데이터가 없습니다.");

        return Convert.FromBase64String(base64);
    }

    private static IReadOnlyList<string> ReadEnumValues(JsonElement supportedParameters, string parameterName)
    {
        if (!supportedParameters.TryGetProperty(parameterName, out var parameter) ||
            !parameter.TryGetProperty("values", out var values) ||
            values.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }

    private static bool ContainsString(JsonElement array, string expected)
    {
        return array.ValueKind == JsonValueKind.Array &&
               array.EnumerateArray().Any(value =>
                   value.ValueKind == JsonValueKind.String &&
                   string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var node) || node.ValueKind != JsonValueKind.String) return false;
        value = node.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string TryReadErrorMessage(string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String) return error.GetString() ?? responseText;
                if (error.TryGetProperty("message", out var message)) return message.GetString() ?? responseText;
            }
        }
        catch (JsonException)
        {
        }

        return string.IsNullOrWhiteSpace(responseText) ? "요청이 실패했습니다." : responseText;
    }

    private sealed class ImageResponse
    {
        [JsonPropertyName("data")]
        public List<ImageData>? Data { get; set; }
    }

    private sealed class ImageData
    {
        [JsonPropertyName("b64_json")]
        public string? Base64 { get; set; }
    }
}
