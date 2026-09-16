using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DKAerialView.Services;

public sealed class OpenRouterImageService
{
    private readonly HttpClient _httpClient;

    public OpenRouterImageService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
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
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ImageResponse>(cancellationToken: cancellationToken);
        var base64 = body?.Data?.FirstOrDefault()?.Base64;
        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("OpenRouter 응답에 이미지 데이터가 없습니다.");

        return Convert.FromBase64String(base64);
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
