using System.Text.Json;

namespace rezepte.Services;

public class YouTubeService
{
    private readonly HttpClient _http;

    // Invidious-Server als Backup-Liste (kein API-Key nötig!)
    private readonly string[] _instances =
    {
        "https://inv.nadeko.net",
        "https://invidious.privacyredirect.com",
        "https://invidious.nerdvpn.de"
    };

    public YouTubeService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(10);
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

    // Holt Video-Infos über Invidious (kein API-Key nötig)
    public async Task<(string Title, string Description, string ThumbnailUrl)?> GetVideoInfoAsync(string videoId)
    {
        foreach (var server in _instances)
        {
            try
            {
                var response = await _http.GetStringAsync($"{server}/api/v1/videos/{videoId}");
                using var doc = JsonDocument.Parse(response);

                var title = doc.RootElement.GetProperty("title").GetString() ?? "";
                var description = doc.RootElement.GetProperty("description").GetString() ?? "";

                // Thumbnail aus dem videoThumbnails Array holen
                var thumbnail = "";
                if (doc.RootElement.TryGetProperty("videoThumbnails", out var thumbs) && thumbs.GetArrayLength() > 0)
                    thumbnail = thumbs[0].GetProperty("url").GetString() ?? "";

                return (title, description, thumbnail);
            }
            catch
            {
                continue; // Nächsten Server versuchen
            }
        }
        return null;
    }
}
