using System.Text.Json;

namespace rezepte.Services;

public class YouTubeService
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;

    // Invidious-Server als Fallback (kein API-Key nötig)
    private readonly string[] _instances =
    {
        "https://inv.nadeko.net",
        "https://yewtu.be",
        "https://invidious.privacyredirect.com",
        "https://iv.ggtyler.dev",
        "https://invidious.nerdvpn.de",
        "https://invidious.io.lol"
    };

    public YouTubeService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(15);
        _apiKey = config["ApiKeys:YouTube"];
    }

    // Extrahiert die Video-ID aus einer YouTube-URL
    public string? ExtractVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        url = url.Trim();

        if (url.Contains("youtu.be/"))
        {
            var start = url.IndexOf("youtu.be/") + 9;
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }
        if (url.Contains("/shorts/"))
        {
            var start = url.IndexOf("/shorts/") + 8;
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }
        if (url.Contains("v="))
        {
            var start = url.IndexOf("v=") + 2;
            var end = url.IndexOfAny(new[] { '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }
        return null;
    }

    // Holt Video-Infos — erst über YouTube API, dann Invidious als Fallback
    public async Task<(string Title, string Description, string ThumbnailUrl)?> GetVideoInfoAsync(string videoId)
    {
        // Offizielle YouTube Data API v3 (wenn Key vorhanden)
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                var url = $"https://www.googleapis.com/youtube/v3/videos?id={videoId}&part=snippet&key={_apiKey}";
                var response = await _http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);

                var items = doc.RootElement.GetProperty("items");
                if (items.GetArrayLength() > 0)
                {
                    var snippet = items[0].GetProperty("snippet");
                    var title       = snippet.GetProperty("title").GetString() ?? "";
                    var description = snippet.GetProperty("description").GetString() ?? "";

                    var thumbnail = "";
                    if (snippet.TryGetProperty("thumbnails", out var thumbs))
                    {
                        // Beste Qualität: maxres → high → medium → default
                        foreach (var quality in new[] { "maxres", "high", "medium", "default" })
                        {
                            if (thumbs.TryGetProperty(quality, out var t))
                            {
                                thumbnail = t.GetProperty("url").GetString() ?? "";
                                break;
                            }
                        }
                    }

                    return (title, description, thumbnail);
                }
            }
            catch
            {
                // Falls YouTube API fehlschlägt → Invidious versuchen
            }
        }

        // Invidious als Fallback
        foreach (var server in _instances)
        {
            try
            {
                var response = await _http.GetStringAsync($"{server}/api/v1/videos/{videoId}");
                using var doc = JsonDocument.Parse(response);

                var title       = doc.RootElement.GetProperty("title").GetString() ?? "";
                var description = doc.RootElement.GetProperty("description").GetString() ?? "";

                var thumbnail = "";
                if (doc.RootElement.TryGetProperty("videoThumbnails", out var thumbs) && thumbs.GetArrayLength() > 0)
                    thumbnail = thumbs[0].GetProperty("url").GetString() ?? "";

                return (title, description, thumbnail);
            }
            catch
            {
                continue;
            }
        }

        return null;
    }
}
