using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace IDESK.Core.Agent;

public class AgentClient
{
    private readonly HttpClient _http = new();

    public async Task<string> SendMessageAsync(string provider, string url, string key, string model, string text, Action<string>? onChunk = null)
    {
        return provider == "anthropic"
            ? await SendAnthropicAsync(url, key, model, text, onChunk)
            : await SendOpenAiAsync(url, key, model, text, onChunk);
    }

    private async Task<string> SendOpenAiAsync(string url, string key, string model, string text, Action<string>? onChunk)
    {
        var body = new
        {
            model,
            stream = true,
            messages = new[] { new { role = "user", content = text } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {key}");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var full = new StringBuilder();
        while (true)
        {
            string? line = await reader.ReadLineAsync();
            if (line == null) break;
            if (line.Length == 0) continue;
            if (!line.StartsWith("data: ")) continue;

            string data = line[6..];
            if (data == "[DONE]") break;

            using var doc = JsonDocument.Parse(data);
            string? chunk = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("delta")
                .GetProperty("content")
                .GetString();

            if (chunk != null)
            {
                full.Append(chunk);
                onChunk?.Invoke(full.ToString());
            }
        }

        return full.ToString();
    }

    private async Task<string> SendAnthropicAsync(string url, string key, string model, string text, Action<string>? onChunk)
    {
        var body = new
        {
            model,
            max_tokens = 1024,
            stream = true,
            messages = new[] { new { role = "user", content = text } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/messages");
        request.Headers.Add("x-api-key", key);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var full = new StringBuilder();
        while (true)
        {
            string? line = await reader.ReadLineAsync();
            if (line == null) break;
            if (line.Length == 0) continue;
            if (!line.StartsWith("data: ")) continue;

            string data = line[6..];
            using var doc = JsonDocument.Parse(data);

            if (doc.RootElement.TryGetProperty("type", out var typeProp))
            {
                string type = typeProp.GetString() ?? "";
                if (type == "content_block_delta" &&
                    doc.RootElement.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("text", out var textProp))
                {
                    full.Append(textProp.GetString());
                    onChunk?.Invoke(full.ToString());
                }
            }
        }

        return full.ToString();
    }
}
