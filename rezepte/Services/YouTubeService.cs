using System.Text.Json;

namespace rezepte.Services;

public class YouTubeService
{
    private readonly HttpClient _http;

    // Invidious + Piped Server (kein API-Key nötig!)
    private readonly string[] _invidiousInstances =
    {
        "https://inv.nadeko.net",
        "https://invidious.privacyredirect.com",
        "https://invidious.nerdvpn.de",
        "https://iv.melmac.space",
        "https://invidious.protokolla.fi",
        "https://inv.tux.pizza",
        "https://invidious.privacydev.net"
    };

    // Piped API als zweite Option
    private readonly string[] _pipedInstances =
    {
        "https://pipedapi.kavin.rocks",
        "https://pipedapi.adminforge.de",
        "https://pipedapi.reallyaweso.me"
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
        if (url.Contains("/embed/"))
        {
            var start = url.IndexOf("/embed/") + 7;
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }
        if (url.Contains("/live/"))
        {
            var start = url.IndexOf("/live/") + 6;
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

    // Holt Video-Infos - zuerst Invidious, dann Piped als Fallback
    public async Task<(string Title, string Description, string ThumbnailUrl)?> GetVideoInfoAsync(string videoId)
    {
        // 1. Invidious versuchen
        foreach (var server in _invidiousInstances)
        {
            try
            {
                var response = await _http.GetStringAsync($"{server}/api/v1/videos/{videoId}");
                using var doc = JsonDocument.Parse(response);

                var title = doc.RootElement.GetProperty("title").GetString() ?? "";
                var description = doc.RootElement.GetProperty("description").GetString() ?? "";

                var thumbnail = "";
                if (doc.RootElement.TryGetProperty("videoThumbnails", out var thumbs) && thumbs.GetArrayLength() > 0)
                    thumbnail = thumbs[0].GetProperty("url").GetString() ?? "";

                return (title, description, thumbnail);
            }
            catch { continue; }
        }

        // 2. Piped API als Fallback
        foreach (var server in _pipedInstances)
        {
            try
            {
                var response = await _http.GetStringAsync($"{server}/streams/{videoId}");
                using var doc = JsonDocument.Parse(response);

                var title = doc.RootElement.GetProperty("title").GetString() ?? "";
                var description = doc.RootElement.GetProperty("description").GetString() ?? "";
                var thumbnail = doc.RootElement.GetProperty("thumbnailUrl").GetString() ?? "";

                return (title, description, thumbnail);
            }
            catch { continue; }
        }

        throw new Exception("Video-Infos konnten nicht abgerufen werden. Bitte später nochmal versuchen.");
    }
}
