using System.Text;
using System.Text.Json;

namespace rezepte.Services;

public class RecipeData
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Ingredients { get; set; } = "";
    public string Steps { get; set; } = "";
}

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["ApiKeys:Groq"]!;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<RecipeData?> ExtractRecipeAsync(string videoTitle, string videoDescription)
    {
        var prompt = $$"""
            Analysiere diesen YouTube-Koch-Video-Titel und die Beschreibung und extrahiere das Rezept.
            Antworte NUR mit einem JSON-Objekt in diesem Format (kein Markdown, kein Text drumherum):
            {
              "title": "Rezeptname",
              "description": "Kurze Beschreibung in 1-2 Sätzen",
              "ingredients": "Zutat 1\nZutat 2\nZutat 3",
              "steps": "Schritt 1\nSchritt 2\nSchritt 3"
            }

            Video-Titel: {{videoTitle}}
            Video-Beschreibung: {{videoDescription}}
            """;

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Status {(int)response.StatusCode}: {responseText}");

        using var doc = JsonDocument.Parse(responseText);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(text)) return null;

        text = text.Replace("```json", "").Replace("```", "").Trim();

        return JsonSerializer.Deserialize<RecipeData>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
