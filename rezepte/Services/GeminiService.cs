/*
 * ============================================================
 * GEMINISERVICE.CS – Dienst zur KI-gestützten Rezeptextraktion
 * ============================================================
 *
 * Dieser Service schickt Titel und Beschreibung eines YouTube-Videos
 * an eine KI (hier: Groq API mit dem Llama-Modell) und bekommt
 * strukturierte Rezeptdaten zurück.
 *
 * Ablauf:
 *   1. Wir bauen einen "Prompt" (Anweisung an die KI)
 *   2. Wir schicken den Prompt als HTTP-Anfrage an die Groq-API
 *   3. Die API antwortet mit JSON (strukturierten Daten)
 *   4. Wir wandeln das JSON in ein C#-Objekt (RecipeData) um
 *
 * Hinweis zum Namen "GeminiService":
 *   Ursprünglich war Google Gemini geplant, aber es wird Groq/Llama verwendet.
 *   Der Name ist einfach nicht geändert worden – das passiert in echten Projekten oft.
 *
 * Dieser Service wird in Program.cs registriert:
 *   builder.Services.AddHttpClient<GeminiService>();
 */

// System.Text – für Encoding (Zeichensatz-Kodierung beim Senden von Daten)
using System.Text;
// System.Text.Json – für das Umwandeln von Objekten in JSON und zurück
using System.Text.Json;

// Namespace: Diese Datei gehört zum Bereich "Services" im Projekt "rezepte"
namespace rezepte.Services;

/*
 * ── HILFSKLASSE: RecipeData ──
 *
 * Diese einfache Klasse speichert das Ergebnis der KI-Antwort.
 * Die KI gibt JSON zurück, das wir direkt in ein RecipeData-Objekt umwandeln.
 *
 * Warum eine eigene Klasse und nicht das Recipe-Model?
 * RecipeData ist einfacher (keine Validierung, keine Datenbankattribute).
 * Sie ist nur ein "Transportbehälter" für die KI-Rohdaten.
 * Danach übertragen wir die Daten ins echte Recipe-Objekt (in Import.cshtml.cs).
 */
public class RecipeData
{
    // Rezeptname (von der KI extrahiert)
    public string Title { get; set; } = "";
    // Kurze Beschreibung des Rezepts
    public string Description { get; set; } = "";
    // Zutaten als Text (mit Zeilenumbrüchen)
    public string Ingredients { get; set; } = "";
    // Zubereitungsschritte als Text (mit Zeilenumbrüchen)
    public string Steps { get; set; } = "";
}

// Hauptklasse des Services
public class GeminiService
{
    /*
     * _http – Der HttpClient für HTTP-Anfragen (kommt per Dependency Injection).
     * _apiKey – Der geheime API-Schlüssel für die Groq-API.
     *
     * "private readonly" = nur intern lesbar, nie veränderbar nach dem Konstruktor.
     * Der API-Schlüssel soll nie geändert werden können – das ist sicherer.
     */
    private readonly HttpClient _http;
    private readonly string _apiKey;

    /*
     * ── KONSTRUKTOR ──
     *
     * Parameter werden per Dependency Injection übergeben:
     *
     * HttpClient http – Der HTTP-Client (von ASP.NET bereitgestellt).
     *
     * IConfiguration config – Das Konfigurationssystem von ASP.NET.
     * IConfiguration liest Werte aus:
     *   - appsettings.json (allgemeine Einstellungen)
     *   - appsettings.Development.json (lokale Einstellungen)
     *   - Umgebungsvariablen (auf dem Server)
     *   - Secrets (für sensible Daten wie API-Keys)
     *
     * config["ApiKeys:Groq"] liest den Wert von ApiKeys → Groq aus der Konfiguration.
     * Das "!" am Ende sagt: "Ich bin sicher, das ist nicht null" (null-forgiving operator).
     * Wenn der Key fehlt, würde die App trotzdem starten, aber beim ersten Aufruf abstürzen.
     *
     * Authorization-Header:
     * Die Groq-API erwartet einen "Bearer Token" zur Authentifizierung.
     * Das ist wie ein Ausweis: "Ich bin berechtigt, diese API zu nutzen."
     * Format: "Authorization: Bearer [API-KEY]"
     * AuthenticationHeaderValue("Bearer", _apiKey) erstellt diesen Header korrekt.
     */
    public GeminiService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["ApiKeys:Groq"]!; // API-Key aus Konfiguration laden

        // Den Authorization-Header für alle Anfragen setzen
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
    }

    /*
     * ── METHODE: ExtractRecipeAsync ──
     *
     * Schickt Videotitel und -beschreibung an die KI und bekommt ein Rezept zurück.
     *
     * "async Task<RecipeData?>" = asynchrone Methode, gibt RecipeData oder null zurück.
     * "?" = Ergebnis kann null sein (wenn KI kein Rezept erkennt oder Fehler auftritt).
     *
     * Parameter:
     *   videoTitle       – Titel des YouTube-Videos
     *   videoDescription – Beschreibung des YouTube-Videos
     */
    public async Task<RecipeData?> ExtractRecipeAsync(string videoTitle, string videoDescription)
    {
        /*
         * ── DEN PROMPT BAUEN ──
         *
         * Ein "Prompt" ist die Anweisung, die wir der KI geben.
         * Je klarer und präziser der Prompt, desto besser die Antwort.
         *
         * "$$"""..."""" ist ein "Raw String Literal" in C# (ab Version 11).
         * Darin können wir Text über mehrere Zeilen schreiben ohne Anführungszeichen zu escapen.
         * "{{variablenname}}" fügt Variablenwerte ein (das $$ macht doppelte { nötig).
         *
         * Wir sagen der KI:
         *   - Was sie analysieren soll (Videotitel und -beschreibung)
         *   - In welchem Format sie antworten soll (JSON)
         *   - Kein Markdown, kein Text drumherum – nur reines JSON
         */
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

        /*
         * ── DEN REQUEST-BODY BAUEN ──
         *
         * Die Groq-API erwartet eine bestimmte JSON-Struktur (OpenAI-kompatibles Format):
         * {
         *   "model": "llama-3.3-70b-versatile",
         *   "messages": [
         *     { "role": "user", "content": "Unser Prompt..." }
         *   ]
         * }
         *
         * "new { ... }" erstellt ein anonymes Objekt (ohne eigene Klasse dafür definieren zu müssen).
         * Das model-Feld gibt an welches KI-Modell benutzt werden soll.
         * "llama-3.3-70b-versatile" ist das Llama 3.3-Modell mit 70 Milliarden Parametern.
         * messages ist ein Array mit einer Nachricht: unsere Frage an die KI.
         * "role: user" bedeutet: diese Nachricht kommt vom Benutzer (nicht vom System).
         */
        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        /*
         * ── DAS OBJEKT IN JSON UMWANDELN ──
         *
         * JsonSerializer.Serialize() wandelt ein C#-Objekt in JSON-Text um.
         * Das nennt man "Serialisierung" (Objekt → Text).
         * Das Gegenteil (Text → Objekt) heißt "Deserialisierung".
         *
         * StringContent erstellt den HTTP-Request-Body:
         *   - json = der JSON-Text
         *   - Encoding.UTF8 = Zeichensatz (UTF-8 ist Standard im Web)
         *   - "application/json" = Content-Type-Header (sagt dem Server: ich schicke JSON)
         */
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        /*
         * ── DIE ANFRAGE AN DIE GROQ-API SCHICKEN ──
         *
         * PostAsync = HTTP POST-Anfrage (wir schicken Daten, nicht nur lesen).
         * Die Groq-API-URL ist kompatibel mit der OpenAI-API (gleiche Struktur).
         * "await" wartet auf die Antwort ohne den Server zu blockieren.
         *
         * response enthält die HTTP-Antwort (Statuscode + Body).
         * responseText ist der Antwort-Body als String (JSON von der KI).
         */
        var response = await _http.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
        var responseText = await response.Content.ReadAsStringAsync();

        /*
         * Statuscode prüfen.
         * IsSuccessStatusCode = true wenn Statuscode 200-299 (z.B. 200 OK).
         * Bei 400, 401, 429 (zu viele Anfragen), 500 usw. ist es false.
         * Dann werfen wir eine Exception mit dem Statuscode und der Fehlermeldung.
         * (int)response.StatusCode wandelt den Enum-Wert in die Zahl um (z.B. 401).
         */
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Status {(int)response.StatusCode}: {responseText}");

        /*
         * ── DIE ANTWORT DER KI PARSEN ──
         *
         * Die Groq-API antwortet mit diesem JSON-Format (OpenAI-kompatibel):
         * {
         *   "choices": [
         *     {
         *       "message": {
         *         "content": "{ \"title\": \"Pasta\", ... }"
         *       }
         *     }
         *   ]
         * }
         *
         * Wir navigieren durch die Struktur:
         *   .GetProperty("choices")    → das "choices"-Array
         *   [0]                        → erstes Element (es gibt immer nur eins)
         *   .GetProperty("message")    → das "message"-Objekt
         *   .GetProperty("content")    → der Text der KI-Antwort
         *   .GetString()               → als C#-String
         */
        using var doc = JsonDocument.Parse(responseText);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        // Wenn die KI keinen Text zurückgegeben hat → null zurückgeben
        if (string.IsNullOrEmpty(text)) return null;

        /*
         * Manchmal gibt die KI trotz unserer Anweisung Markdown zurück:
         * ```json
         * { "title": "..." }
         * ```
         * Wir entfernen diese Markdown-Codeblöcke:
         *   Replace("```json", "") – entfernt den Markdown-Start
         *   Replace("```", "")     – entfernt den Markdown-Ende-Block
         *   Trim()                 – entfernt Leerzeichen am Anfang und Ende
         */
        text = text.Replace("```json", "").Replace("```", "").Trim();

        /*
         * ── JSON IN EIN OBJEKT UMWANDELN (Deserialisierung) ──
         *
         * JsonSerializer.Deserialize<RecipeData>() wandelt den JSON-Text
         * in ein RecipeData-Objekt um. Die Eigenschaftsnamen im JSON müssen zu
         * den Eigenschaftsnamen in RecipeData passen (oder wir setzen Optionen).
         *
         * PropertyNameCaseInsensitive = true:
         *   - "title", "Title" und "TITLE" werden alle der Eigenschaft "Title" zugeordnet.
         *   - Macht die Deserialisierung robuster gegenüber KI-Unregelmäßigkeiten.
         *
         * Wenn das JSON nicht gültig ist oder nicht passt, gibt Deserialize null zurück.
         */
        return JsonSerializer.Deserialize<RecipeData>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}
