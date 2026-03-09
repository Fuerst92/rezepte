/*
 * ============================================================
 * YOUTUBESERVICE.CS – Dienst zum Abrufen von YouTube-Videodaten
 * ============================================================
 *
 * Was ist ein "Service"?
 * ──────────────────────
 * Ein Service (Dienst) ist eine Klasse, die eine bestimmte Aufgabe erledigt –
 * und zwar diese Aufgabe von überall in der App benutzbar macht.
 * Man lagert Logik in Services aus, damit:
 *   1. Der Code übersichtlich bleibt (Trennung der Verantwortlichkeiten)
 *   2. Derselbe Code an vielen Stellen benutzt werden kann (nicht kopieren!)
 *   3. Die Klassen leichter testbar sind
 *
 * Dieser YouTubeService macht genau eine Sache:
 *   → Gegeben eine YouTube-URL: Gib mir Titel, Beschreibung und Thumbnail-URL zurück.
 *
 * Er benutzt dafür KEINEN offiziellen API-Key von Google, sondern:
 *   1. Das öffentliche oEmbed-Endpunkt (kostenlos, kein Key nötig)
 *   2. Das direkte Lesen der YouTube-HTML-Seite (Web-Scraping)
 *
 * Dieser Service wird in Program.cs registriert:
 *   builder.Services.AddHttpClient<YouTubeService>();
 * Und per Dependency Injection in ImportModel benutzt (siehe Import.cshtml.cs).
 */

// System.Text.Json – Bibliothek zum Parsen von JSON-Antworten (API-Daten kommen oft als JSON)
using System.Text.Json;
// System.Text.RegularExpressions – Bibliothek für reguläre Ausdrücke (Muster-Suche in Texten)
using System.Text.RegularExpressions;

// Namespace: Diese Klasse gehört zum Bereich "Services" im Projekt "rezepte"
namespace rezepte.Services;

// "public class YouTubeService" – öffentlich zugängliche Serviceklasse
public class YouTubeService
{
    /*
     * _http – Das HttpClient-Objekt zum Senden von HTTP-Anfragen.
     *
     * "private" = nur innerhalb dieser Klasse zugreifbar (von außen unsichtbar).
     * "readonly" = kann nach dem Konstruktor nicht mehr geändert werden (Sicherheit).
     * "_" am Anfang = Konvention für private Felder (zeigt: das ist intern).
     *
     * HttpClient ist wie ein Browser für Code: Er kann URLs aufrufen und
     * die Antwort (HTML, JSON usw.) zurückgeben.
     */
    private readonly HttpClient _http;

    /*
     * ── KONSTRUKTOR ──
     *
     * Der Konstruktor wird aufgerufen, wenn das Objekt erstellt wird.
     * Hier: "new YouTubeService(http)" – ASP.NET macht das automatisch via Dependency Injection.
     *
     * HttpClient http – Der HttpClient wird von außen übergeben (Dependency Injection).
     * Das ist wichtig: YouTubeService erstellt keinen eigenen HttpClient,
     * er bekommt einen von ASP.NET. Das vermeidet typische Fehler (Socket-Erschöpfung)
     * und macht den Code testbarer.
     *
     * Hier konfigurieren wir den HttpClient direkt nach der Übergabe:
     */
    public YouTubeService(HttpClient http)
    {
        _http = http;

        /*
         * Timeout = 15 Sekunden.
         * Wenn YouTube nach 15 Sekunden nicht antwortet, bricht die Anfrage ab.
         * Ohne Timeout könnte die App endlos warten (was den Browser des Benutzers einfriert).
         * TimeSpan.FromSeconds(15) erstellt eine Zeitspanne von 15 Sekunden.
         */
        _http.Timeout = TimeSpan.FromSeconds(15);

        /*
         * User-Agent – Simuliert einen normalen Webbrowser.
         * YouTube blockiert Anfragen die aussehen wie automatisierte Programme.
         * Der User-Agent-Header sagt YouTube: "Ich bin ein Chrome-Browser auf Windows."
         * Ohne diesen Header würde YouTube unsere Anfrage ablehnen (403 Forbidden).
         *
         * DefaultRequestHeaders bedeutet: Sende diesen Header bei JEDER Anfrage mit.
         */
        // Normalen Browser simulieren damit YouTube antwortet
        _http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        /*
         * Accept-Language – Sagt dem Server: Wir bevorzugen deutsche Inhalte.
         * "de-DE,de;q=0.9" bedeutet: Deutsch (Deutschland) ist bevorzugt,
         * Deutsch allgemein als zweite Wahl (q=0.9 = 90% Präferenz).
         */
        _http.DefaultRequestHeaders.Add("Accept-Language", "de-DE,de;q=0.9");
    }

    /*
     * ── METHODE: ExtractVideoId ──
     *
     * Extrahiert die Video-ID aus einer YouTube-URL.
     * YouTube-Videos haben immer eine eindeutige ID, z.B. "dQw4w9WgXcQ".
     * Diese ID steckt in der URL an verschiedenen Stellen, je nach URL-Format.
     *
     * Rückgabewert: string? – ein String oder null.
     *   - null = keine gültige YouTube-URL erkannt
     *   - "dQw4w9WgXcQ" = die Video-ID
     *
     * Unterstützte URL-Formate:
     *   - https://youtu.be/dQw4w9WgXcQ          (Kurzlink)
     *   - https://youtube.com/shorts/dQw4w9WgXcQ (Shorts)
     *   - https://youtube.com/embed/dQw4w9WgXcQ  (Einbettung)
     *   - https://youtube.com/watch?v=dQw4w9WgXcQ (Standard)
     */
    // Video-ID aus URL extrahieren
    public string? ExtractVideoId(string url)
    {
        /*
         * string.IsNullOrWhiteSpace prüft ob der String:
         *   - null ist, ODER
         *   - leer ist (""), ODER
         *   - nur Leerzeichen enthält ("   ")
         * Wenn ja, gibt es keine ID → return null.
         */
        if (string.IsNullOrWhiteSpace(url)) return null;

        // Leerzeichen am Anfang und Ende entfernen (falls jemand reinkopiert hat)
        url = url.Trim();

        /*
         * Für jeden URL-Typ: Suche das charakteristische Muster und extrahiere die ID danach.
         *
         * IndexOf("youtu.be/") gibt die Position des Musters zurück.
         * + 9 springt hinter das Muster (9 = Länge von "youtu.be/").
         * IndexOfAny(new[] { '?', '&', '#' }, start) sucht das Ende der ID
         * (ID endet bei '?', '&' oder '#' in der URL).
         * Wenn kein Endzeichen gefunden wird (end == -1): ID geht bis zum String-Ende (url[start..]).
         * Sonst: ID ist der Teil von start bis end (url[start..end]).
         *
         * url[start..end] ist C# Range-Syntax – entspricht url.Substring(start, end - start).
         */

        // Format: youtu.be/VIDEO_ID
        if (url.Contains("youtu.be/"))
        {
            var start = url.IndexOf("youtu.be/") + 9; // 9 = Länge von "youtu.be/"
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // Format: youtube.com/shorts/VIDEO_ID
        if (url.Contains("/shorts/"))
        {
            var start = url.IndexOf("/shorts/") + 8; // 8 = Länge von "/shorts/"
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // Format: youtube.com/embed/VIDEO_ID
        if (url.Contains("/embed/"))
        {
            var start = url.IndexOf("/embed/") + 7; // 7 = Länge von "/embed/"
            var end = url.IndexOfAny(new[] { '?', '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // Format: youtube.com/watch?v=VIDEO_ID
        if (url.Contains("v="))
        {
            var start = url.IndexOf("v=") + 2; // 2 = Länge von "v="
            var end = url.IndexOfAny(new[] { '&', '#' }, start);
            return end == -1 ? url[start..] : url[start..end];
        }

        // Kein bekanntes Format gefunden → null zurückgeben
        return null;
    }

    /*
     * ── METHODE: GetVideoInfoAsync ──
     *
     * Holt Titel, Beschreibung und Thumbnail-URL für ein YouTube-Video.
     *
     * "async Task<...>" bedeutet: Diese Methode ist asynchron.
     * Was bedeutet "asynchron"?
     *   - Wenn die Methode auf eine Antwort von YouTube wartet (Netzwerkanfrage),
     *     blockiert sie den Thread NICHT.
     *   - Der Server kann in der Wartezeit andere Anfragen bearbeiten.
     *   - "await" markiert die Stellen, an denen gewartet wird.
     *   - Ohne async/await würde jede Netzwerkanfrage den Server einfrieren.
     *
     * Rückgabewert: (string Title, string Description, string ThumbnailUrl)?
     *   - Das ist ein "Tuple" (mehrere Werte auf einmal zurückgeben).
     *   - Das "?" macht das gesamte Tuple nullable (kann null sein).
     *   - Wenn das Video nicht gefunden wird, wirft die Methode eine Exception.
     */
    // Video-Infos direkt von YouTube holen (kein API-Key nötig)
    public async Task<(string Title, string Description, string ThumbnailUrl)?> GetVideoInfoAsync(string videoId)
    {
        /*
         * ── SCHRITT 1: Titel und Thumbnail über oEmbed ──
         *
         * oEmbed ist ein offenes Protokoll – YouTube bietet es kostenlos und ohne API-Key an.
         * Es gibt minimale Infos über ein Video zurück (Titel, Thumbnail-URL, Embed-Code).
         * URL-Format: youtube.com/oembed?url=[video-url]&format=json
         */
        // Schritt 1: Titel + Thumbnail über oEmbed (offizielle kostenlose YouTube API)
        string title = "";
        // Standard-Thumbnail-URL (funktioniert immer, wenn man die Video-ID kennt)
        // hqdefault.jpg = "High Quality Default" Thumbnail
        string thumbnail = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";

        /*
         * try { } catch { } – Fehlerbehandlung (Exception Handling).
         * Alles im try-Block wird ausgeführt.
         * Wenn ein Fehler passiert, springt die Ausführung in den catch-Block.
         * Hier: Wenn oEmbed fehlschlägt, machen wir weiter mit leerem Titel.
         * Der catch-Block ist leer = Fehler still ignorieren und weitermachen.
         */
        try
        {
            // oEmbed-URL zusammenbauen (Interpolation mit $"...")
            var oembedUrl = $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json";

            // HTTP GET-Anfrage senden und Antwort als String empfangen
            // "await" wartet auf die Antwort ohne den Server zu blockieren
            var oembedJson = await _http.GetStringAsync(oembedUrl);

            /*
             * JsonDocument.Parse(oembedJson) – JSON-Text in ein durchsuchbares Objekt umwandeln.
             * "using var doc = ..." stellt sicher, dass der Speicher nach dem Block freigegeben wird.
             *
             * JSON-Antwort sieht z.B. so aus:
             * {
             *   "title": "Mein Koch-Video",
             *   "thumbnail_url": "https://i.ytimg.com/vi/abc/hqdefault.jpg",
             *   ...
             * }
             *
             * doc.RootElement = das Wurzelobjekt (die äußersten { })
             * .GetProperty("title") = die Eigenschaft "title" lesen
             * .GetString() = als C#-String zurückgeben
             * ?? "" = falls der Wert null ist, nimm leeren String
             */
            using var doc = JsonDocument.Parse(oembedJson);
            title = doc.RootElement.GetProperty("title").GetString() ?? "";

            // TryGetProperty – sicher lesen: gibt false zurück wenn Eigenschaft fehlt (kein Fehler!)
            if (doc.RootElement.TryGetProperty("thumbnail_url", out var thumb))
                thumbnail = thumb.GetString() ?? thumbnail; // Thumbnail-URL aus oEmbed übernehmen
        }
        catch
        {
            // oEmbed fehlgeschlagen, weiter mit leerem Titel
        }

        /*
         * ── SCHRITT 2: Beschreibung aus der YouTube-Seite ──
         *
         * Die YouTube-Seite enthält versteckte Meta-Tags mit Informationen:
         * <meta property="og:description" content="Hier ist die Beschreibung...">
         * "og:" steht für Open Graph – ein Standard für Social-Media-Metadaten.
         *
         * Wir laden die HTML-Seite des Videos und durchsuchen sie nach diesen Tags.
         * Das nennt man "Web-Scraping" – Daten aus einer Webseite extrahieren.
         */
        // Schritt 2: Beschreibung aus der YouTube-Seite extrahieren
        string description = "";
        try
        {
            // YouTube-Seite des Videos laden
            var pageUrl = $"https://www.youtube.com/watch?v={videoId}";
            var html = await _http.GetStringAsync(pageUrl); // HTML-Code der Seite als String

            /*
             * Regex (Regulärer Ausdruck) – ein mächtiges Werkzeug zur Muster-Suche in Texten.
             *
             * Das Muster sucht nach: <meta name="og:description" content="[INHALT]">
             * oder:                  <meta property="og:description" content="[INHALT]">
             *
             * ([^"]*) ist die Capture-Group – das was wir haben wollen (der Inhalt).
             * RegexOptions.IgnoreCase = Groß-/Kleinschreibung ignorieren
             *
             * Regex ist komplexer Stoff – für Anfänger: stell dir vor es ist eine sehr
             * präzise Suchfunktion, die Platzhalter und Muster verstehen kann.
             */
            // og:description Meta-Tag auslesen
            var metaMatch = Regex.Match(html,
                @"<meta\s+(?:name|property)=""og:description""\s+content=""([^""]*)""\s*/?>",
                RegexOptions.IgnoreCase);

            // Zweite Variante versuchen falls erste nicht gefunden (Attributreihenfolge anders)
            if (!metaMatch.Success)
                metaMatch = Regex.Match(html,
                    @"<meta\s+content=""([^""]*)""\s+(?:name|property)=""og:description""\s*/?>",
                    RegexOptions.IgnoreCase);

            if (metaMatch.Success)
                /*
                 * HtmlDecode – wandelt HTML-Sonderzeichen zurück in normale Zeichen.
                 * z.B.: "&amp;" → "&", "&lt;" → "<", "&#39;" → "'"
                 * Ohne HtmlDecode würden diese Zeichen falsch angezeigt.
                 * Groups[1] = der erste Klammerausdruck in der Regex = unser Inhalt
                 */
                description = System.Net.WebUtility.HtmlDecode(metaMatch.Groups[1].Value);

            // Falls oEmbed keinen Titel lieferte, aus dem HTML-Titel lesen
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

        /*
         * Wenn nach beiden Schritten kein Titel gefunden wurde:
         * Das Video existiert nicht, ist privat, oder YouTube hat uns blockiert.
         * Wir werfen eine Exception (Ausnahme) mit einer verständlichen Fehlermeldung.
         * Der Aufrufer (ImportModel) fängt diese Exception im try/catch auf.
         *
         * "throw new Exception(...)" – erzeugt einen Fehler und bricht die Methode ab.
         */
        if (string.IsNullOrEmpty(title))
            throw new Exception("Video nicht gefunden oder nicht erreichbar.");

        // Titel, Beschreibung und Thumbnail als Tuple zurückgeben
        return (title, description, thumbnail);
    }
}
