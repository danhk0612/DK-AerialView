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
    IReadOnlyList<string> AspectRatios,
    IReadOnlyList<string> OutputFormats)
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
            var outputFormats = ReadEnumValues(supportedParameters, "output_format");
            if (outputFormats.Count == 1 && string.Equals(outputFormats[0], "svg", StringComparison.OrdinalIgnoreCase))
                continue;

            models.Add(new OpenRouterImageModel(id, name, maxInputReferences, resolutions, aspectRatios, outputFormats));
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
        var availableModels = await GetEditingModelsAsync(apiKey, cancellationToken);
        var capability = availableModels.FirstOrDefault(item => string.Equals(item.Id, model, StringComparison.OrdinalIgnoreCase));
        if (capability is null)
            throw new InvalidOperationException("선택한 모델이 현재 OpenRouter에서 참조 이미지 편집을 지원하지 않습니다. 설정에서 모델 목록을 다시 불러오세요.");

        var dataUrl = $"data:image/png;base64,{Convert.ToBase64String(sourceImage)}";
        var request = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["prompt"] = prompt,
            ["input_references"] = new[]
            {
                new
                {
                    type = "image_url",
                    image_url = new { url = dataUrl }
                }
            }
        };

        var selectedResolution = SelectResolution(capability.Resolutions, resolution);
        if (selectedResolution is not null)
            request["resolution"] = selectedResolution;

        if (capability.AspectRatios.Contains(aspectRatio, StringComparer.OrdinalIgnoreCase))
            request["aspect_ratio"] = aspectRatio;
        else if (capability.AspectRatios.Contains("auto", StringComparer.OrdinalIgnoreCase))
            request["aspect_ratio"] = "auto";

        if (capability.OutputFormats.Contains("png", StringComparer.OrdinalIgnoreCase))
            request["output_format"] = "png";

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

    private static string? SelectResolution(IReadOnlyList<string> supported, string requested)
    {
        if (supported.Count == 0) return null;
        var exact = supported.FirstOrDefault(value => string.Equals(value, requested, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        var requestedRank = ResolutionRank(requested);
        return supported
            .Select(value => new { Value = value, Rank = ResolutionRank(value) })
            .Where(item => item.Rank > 0 && item.Rank <= requestedRank)
            .OrderByDescending(item => item.Rank)
            .Select(item => item.Value)
            .FirstOrDefault() ?? supported[0];
    }

    private static int ResolutionRank(string value) => value.ToUpperInvariant() switch
    {
        "512" => 1,
        "1K" => 2,
        "2K" => 3,
        "4K" => 4,
        _ => 0
    };

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
