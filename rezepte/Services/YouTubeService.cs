using System.Text.Json;
using System.Text.RegularExpressions;

namespace rezepte.Services;

public class YouTubeService
{
    private readonly HttpClient _http;

    public YouTubeService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(15);
        // Normalen Browser simulieren damit YouTube antwortet
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _http.DefaultRequestHeaders.Add("Accept-Language", "de-DE,de;q=0.9");
    }

    // Video-ID aus URL extrahieren
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
        if (url.Contains("v="))
        {
            var start = url.IndexOf("v=") + 2;
            var end = url.IndexOfAny(new[] { '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }
        return null;
    }

    // Video-Infos direkt von YouTube holen (kein API-Key nötig)
    public async Task<(string Title, string Description, string ThumbnailUrl)?> GetVideoInfoAsync(string videoId)
    {
        // Schritt 1: Titel + Thumbnail über oEmbed (offizielle kostenlose YouTube API)
        string title = "";
        string thumbnail = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

        try
        {
            var oembedUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";
            var oembedJson = await _http.GetStringAsync(oembedUrl);
            using var doc = JsonDocument.Parse(oembedJson);
            title = doc.RootElement.GetProperty("title").GetString() ?? "";
            if (doc.RootElement.TryGetProperty("thumbnail_url", out var thumb))
                thumbnail = thumb.GetString() ?? thumbnail;
        }
        catch
        {
            // oEmbed fehlgeschlagen, weiter mit leerem Titel
        }

        // Schritt 2: Beschreibung aus der YouTube-Seite extrahieren
        string description = "";
        try
        {
            var pageUrl = $"https://www.youtube.com/watch?v={videoId}";
            var html = await _http.GetStringAsync(pageUrl);

            // og:description Meta-Tag auslesen
            var metaMatch = Regex.Match(html,
                @"<meta\s+(?:name|property)=""og:description""\s+content=""([^""]*)""\s*/?>",
                RegexOptions.IgnoreCase);

            if (!metaMatch.Success)
                metaMatch = Regex.Match(html,
                    @"<meta\s+content=""([^""]*)""\s+(?:name|property)=""og:description""\s*/?>",
                    RegexOptions.IgnoreCase);

            if (metaMatch.Success)
                description = System.Net.WebUtility.HtmlDecode(metaMatch.Groups[1].Value);

            // Titel aus HTML falls oEmbed fehlschlug
            if (string.IsNullOrEmpty(title))
            {
                var titleMatch = Regex.Match(html,
                    @"<meta\s+(?:name|property)=""og:title""\s+content=""([^""]*)""\s*/?>",
                    RegexOptions.IgnoreCase);
                if (titleMatch.Success)
                    title = System.Net.WebUtility.HtmlDecode(titleMatch.Groups[1].Value);
            }
        }
        catch
        {
            // HTML-Parsing fehlgeschlagen
        }

        if (string.IsNullOrEmpty(title))
            throw new Exception("Video nicht gefunden oder nicht erreichbar.");

        return (title, description, thumbnail);
    }
}
