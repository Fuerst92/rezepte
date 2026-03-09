using System.Text.Json;

namespace rezepte.Services;

public class YouTubeService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public YouTubeService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["ApiKeys:YouTube"]!;
    }

    // Extrahiert die Video-ID aus einer YouTube-URL
    // Unterstützt: watch?v=, youtu.be/, /shorts/, /embed/, /live/
    public string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        url = url.Trim();

        // youtu.be/VIDEO_ID
        if (url.Contains("youtu.be/"))
        {
            var start = url.IndexOf("youtu.be/") + 9;
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // /shorts/VIDEO_ID
        if (url.Contains("/shorts/"))
        {
            var start = url.IndexOf("/shorts/") + 8;
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // /embed/VIDEO_ID
        if (url.Contains("/embed/"))
        {
            var start = url.IndexOf("/embed/") + 7;
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // /live/VIDEO_ID
        if (url.Contains("/live/"))
        {
            var start = url.IndexOf("/live/") + 6;
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // watch?v=VIDEO_ID oder ?v=VIDEO_ID
        if (url.Contains("v="))
        {
            var start = url.IndexOf("v=") + 2;
            var end = url.IndexOfAny(new[] { '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        return null;
    }

    // Ruft Titel und Beschreibung des Videos von der YouTube API ab
    public async Task<(string Title, string Description, string ThumbnailUrl)?> GetVideoInfoAsync(string videoId)
    {
        var url = $"https://www.googleapis.com/youtube/v3/videos?part=snippet&id={videoId}&key={_apiKey}";
        var response = await _http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(response);
        var items = doc.RootElement.GetProperty("items");

        if (items.GetArrayLength() == 0) return null;

        var snippet = items[0].GetProperty("snippet");
        var title = snippet.GetProperty("title").GetString() ?? "";
        var description = snippet.GetProperty("description").GetString() ?? "";
        var thumbnail = snippet.GetProperty("thumbnails")
                               .GetProperty("high")
                               .GetProperty("url")
                               .GetString() ?? "";

        return (title, description, thumbnail);
    }
}
