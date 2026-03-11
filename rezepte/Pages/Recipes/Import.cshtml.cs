/*
 * ============================================================
 * IMPORT.CSHTML.CS – PageModel für die Import-Seite
 * ============================================================
 *
 * Was ist eine "cshtml.cs"-Datei?
 * ─────────────────────────────────
 * In Razor Pages gibt es immer zwei Dateien zusammen:
 *   - Import.cshtml     → das HTML-Template (was der Benutzer sieht)
 *   - Import.cshtml.cs  → der C#-Code dahinter (diese Datei, die Logik)
 *
 * Die .cs-Datei heißt "PageModel" (hier: ImportModel).
 * Sie behandelt:
 *   - Was passiert wenn die Seite aufgerufen wird (GET-Request)?
 *   - Was passiert wenn ein Formular abgeschickt wird (POST-Request)?
 *
 * Diese Import-Seite erlaubt das Importieren von Rezepten aus:
 *   1. YouTube-Videos (Titel + Beschreibung → KI extrahiert Rezept)
 *   2. Social Media Captions (Instagram/TikTok-Text → KI extrahiert Rezept)
 *
 * Ablauf für YouTube:
 *   1. Benutzer gibt YouTube-URL ein und klickt "Analysieren"
 *   2. OnPostAnalyzeAsync() wird aufgerufen
 *   3. YouTubeService holt Titel + Beschreibung vom Video
 *   4. GeminiService schickt das an die KI und bekommt strukturiertes Rezept
 *   5. Das Formular wird mit den KI-Daten vorausgefüllt angezeigt
 *   6. Benutzer prüft/bearbeitet und klickt "Speichern"
 *   7. OnPostSaveAsync() speichert das Rezept in der Datenbank
 */

// using = Bibliotheken einbinden (ohne diese würde der Code die Klassen nicht kennen)
using Microsoft.AspNetCore.Mvc;           // IActionResult, RedirectToPage, Page usw.
using Microsoft.AspNetCore.Mvc.RazorPages; // PageModel (Basisklasse für Razor Pages)
using Microsoft.AspNetCore.Mvc.Rendering; // SelectList (für Dropdown-Menüs)
using rezepte.Data;                        // AppDbContext (Datenbankzugriff)
using rezepte.Models;                      // Recipe, Category (Datenmodelle)
using rezepte.Services;                    // YouTubeService, GeminiService

/*
 * Namespace: Diese Datei gehört zu Pages → Recipes
 * Das entspricht der Ordnerstruktur des Projekts.
 */
namespace rezepte.Pages.Recipes;

/*
 * ImportModel erbt von PageModel.
 * "erbt" (: PageModel) bedeutet: ImportModel bekommt automatisch alle Funktionen von PageModel.
 * PageModel ist die Basisklasse für alle Razor Page Models in ASP.NET.
 * Sie stellt z.B. Page(), RedirectToPage() und ModelState zur Verfügung.
 */
public class ImportModel : PageModel
{
    /*
     * ── ABHÄNGIGKEITEN (Dependencies) ──
     *
     * Diese drei privaten Felder sind die "Werkzeuge", die diese Klasse braucht.
     * Sie werden per Dependency Injection (DI) im Konstruktor übergeben.
     *
     * _db      = Datenbankzugriff (Lesen und Speichern von Rezepten/Kategorien)
     * _youtube = Service zum Abrufen von YouTube-Videodaten
     * _gemini  = Service für KI-gestützte Rezeptextraktion
     *
     * "private readonly" = nur intern lesbar, unveränderlich nach Zuweisung im Konstruktor.
     */
    private readonly AppDbContext _db;
    private readonly YouTubeService _youtube;
    private readonly GeminiService _gemini;

    /*
     * ── KONSTRUKTOR mit Dependency Injection ──
     *
     * ASP.NET ruft diesen Konstruktor automatisch auf, wenn die Seite aufgerufen wird.
     * Die drei Parameter (db, youtube, gemini) werden automatisch vom DI-Container geliefert,
     * weil wir sie in Program.cs registriert haben:
     *   - AddDbContext<AppDbContext>()
     *   - AddHttpClient<YouTubeService>()
     *   - AddHttpClient<GeminiService>()
     *
     * Wir speichern die übergebenen Objekte in den privaten Feldern,
     * damit alle Methoden dieser Klasse sie benutzen können.
     */
    public ImportModel(AppDbContext db, YouTubeService youtube, GeminiService gemini)
    {
        _db = db;
        _youtube = youtube;
        _gemini = gemini;
    }

    /*
     * ── PROPERTIES MIT [BindProperty] ──
     *
     * Was bedeutet [BindProperty]?
     * ─────────────────────────────
     * Wenn ein HTML-Formular abgeschickt wird (POST-Request), sendet der Browser
     * die eingegebenen Werte als Schlüssel-Wert-Paare mit.
     * [BindProperty] sagt ASP.NET: "Fülle diese Property automatisch mit dem
     * Formularwert, der denselben Namen hat."
     *
     * Beispiel: <input name="YoutubeUrl" value="https://..." />
     * → ASP.NET setzt automatisch: YoutubeUrl = "https://..."
     *
     * Ohne [BindProperty] müsste man das manuell aus dem Request lesen:
     *   string url = Request.Form["YoutubeUrl"];
     *
     * YouTube-Tab: Die eingegebene YouTube-URL
     */
    // YouTube
    [BindProperty] public string YoutubeUrl { get; set; } = "";

    /*
     * Social Media Tab (Instagram/TikTok):
     *   SocialUrl     = Link zum Social-Media-Post (optional)
     *   SocialCaption = Text/Caption des Posts (hier ist das Rezept drin)
     */
    // Instagram / TikTok
    [BindProperty] public string SocialUrl { get; set; } = "";
    [BindProperty] public string SocialCaption { get; set; } = "";

    /*
     * Das Recipe-Objekt für das Formular.
     * [BindProperty] bedeutet hier: alle Felder von Recipe werden aus dem Formular befüllt.
     * Das ist mächtig: ASP.NET befüllt automatisch Recipe.Title, Recipe.Ingredients usw.
     * aus den Formularfeldern mit den entsprechenden Namen.
     *
     * "= new()" erstellt ein leeres Recipe-Objekt als Standardwert
     * (damit recipe.Title nicht null ist, bevor das Formular abgeschickt wird).
     */
    [BindProperty] public Recipe Recipe { get; set; } = new();

    /*
     * CategoryOptions – Dropdown-Liste für Kategorien.
     * SelectList ist eine ASP.NET-Klasse speziell für <select>-Elemente (Dropdowns).
     * "null!" = wird immer in LoadCategories() gesetzt, bevor es benutzt wird.
     *
     * Diese Property hat kein [BindProperty], weil sie nur aus der DB geladen wird
     * und kein Formularfeld ist – sie ist nur für das HTML-Template.
     */
    public SelectList CategoryOptions { get; set; } = null!;

    /*
     * Statusvariablen für das Template:
     *   ErrorMessage  = Fehlermeldung (z.B. "Ungültige URL") oder null wenn kein Fehler
     *   RecipeLoaded  = true, wenn die KI ein Rezept extrahiert hat → Formular anzeigen
     *   VideoId       = YouTube-Video-ID für das Einbetten des Videos auf der Seite
     *   ActiveTab     = welcher Tab gerade aktiv ist ("youtube" oder "social")
     */
    public string? ErrorMessage { get; set; }
    public bool RecipeLoaded { get; set; } = false;
    public string? VideoId { get; set; }
    public string ActiveTab { get; set; } = "youtube"; // Standard: YouTube-Tab ist aktiv

    /*
     * ── OnGet() ──
     *
     * Wird aufgerufen wenn die Seite zum ersten Mal aufgerufen wird (GET-Request).
     * Zum Beispiel wenn der Benutzer auf den Link /Recipes/Import klickt.
     *
     * Hier laden wir nur die Kategorien für das Dropdown – mehr passiert nicht.
     * Das Formular ist leer, RecipeLoaded = false (kein Rezept geladen).
     *
     * "void" = diese Methode gibt nichts zurück (kein Rückgabewert).
     */
    public void OnGet()
    {
        LoadCategories(); // Kategorien aus der Datenbank laden
    }

    /*
     * ── OnPostAnalyzeAsync() – YouTube analysieren ──
     *
     * Wird aufgerufen wenn der Benutzer auf "YouTube analysieren" klickt.
     * "Post" im Methodennamen = reagiert auf POST-Request.
     * "Analyze" ist der "handler name" – im HTML-Formular steht: asp-page-handler="Analyze"
     * "Async" = die Methode ist asynchron (wartet auf Netzwerkantworten).
     *
     * Rückgabewert: Task<IActionResult>
     *   - Task = asynchrones Ergebnis (wegen async/await)
     *   - IActionResult = Antwort-Typ (Page() zeigt Seite, RedirectToPage() leitet weiter)
     */
    // ── YouTube analysieren ──
    public async Task<IActionResult> OnPostAnalyzeAsync()
    {
        LoadCategories(); // Kategorien laden (für das Dropdown nach dem Post)
        ActiveTab = "youtube"; // YouTube-Tab bleibt aktiv

        /*
         * Schritt 1: Video-ID aus der URL extrahieren.
         * ExtractVideoId gibt null zurück wenn die URL kein gültiger YouTube-Link ist.
         * Dann: Fehlermeldung setzen und die Seite nochmal anzeigen (return Page()).
         *
         * "return Page()" = zeige dieselbe Seite nochmal an (mit der Fehlermeldung).
         */
        var videoId = _youtube.ExtractVideoId(YoutubeUrl);
        if (videoId == null)
        {
            ErrorMessage = "Kein gültiger YouTube-Link. Bitte prüfe die URL.";
            return Page();
        }

        /*
         * Schritt 2: Video-Infos von YouTube abrufen (Titel, Beschreibung, Thumbnail).
         * try/catch: Wenn YouTube nicht erreichbar ist oder das Video privat ist,
         * fangen wir den Fehler auf und zeigen eine verständliche Meldung.
         */
        (string Title, string Description, string ThumbnailUrl)? videoInfo;
        try
        {
            videoInfo = await _youtube.GetVideoInfoAsync(videoId);
        }
        catch (Exception ex)
        {
            // ex.Message ist die Fehlermeldung aus der Exception
            ErrorMessage = $"YouTube API Fehler: {ex.Message}";
            return Page();
        }

        // null-Check: Video nicht gefunden
        if (videoInfo == null)
        {
            ErrorMessage = "Video nicht gefunden. Bitte prüfe den Link.";
            return Page();
        }

        /*
         * Schritt 3: Rezept mit KI extrahieren.
         * Wir übergeben Titel und Beschreibung des Videos an GeminiService.
         * Der Service schickt es an die Groq-KI und gibt ein RecipeData-Objekt zurück.
         *
         * ".Value" ist nötig, weil videoInfo ein nullable Tuple ist (videoInfo?).
         * Mit .Value greifen wir auf das eigentliche Tuple zu.
         */
        RecipeData? recipeData;
        try
        {
            recipeData = await _gemini.ExtractRecipeAsync(videoInfo.Value.Title, videoInfo.Value.Description);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"KI Fehler: {ex.Message}";
            return Page();
        }

        // KI hat kein Rezept gefunden (z.B. kein Koch-Video)
        if (recipeData == null)
        {
            ErrorMessage = "KI konnte kein Rezept aus dem Video extrahieren.";
            return Page();
        }

        /*
         * Schritt 4: Daten von RecipeData in das Recipe-Objekt übertragen.
         * Recipe ist das Formular-Objekt das dem Benutzer angezeigt wird.
         * Der Benutzer kann die KI-Daten dann noch bearbeiten bevor er speichert.
         */
        Recipe.Title = recipeData.Title;
        Recipe.Description = recipeData.Description;
        Recipe.Ingredients = recipeData.Ingredients;
        Recipe.Steps = recipeData.Steps;
        Recipe.Equipment = string.IsNullOrWhiteSpace(recipeData.Equipment) ? null : recipeData.Equipment;
        Recipe.Servings = recipeData.Servings > 0 ? recipeData.Servings : 4;
        Recipe.ImageUrl = videoInfo.Value.ThumbnailUrl;
        Recipe.VideoUrl = YoutubeUrl;

        // VideoId für den Einbettungslink auf der Seite speichern
        VideoId = videoId;
        // RecipeLoaded = true → das Formular wird jetzt angezeigt
        RecipeLoaded = true;
        return Page(); // Seite nochmal anzeigen, diesmal mit vorausgefülltem Formular
    }

    /*
     * ── OnPostAnalyzeSocialAsync() – Social Media analysieren ──
     *
     * Ähnlich wie YouTube, aber für Instagram/TikTok.
     * Hier gibt es keine Video-ID – nur den Text/Caption des Posts.
     * Die Caption enthält das Rezept in Textform.
     *
     * "AnalyzeSocial" = Handler-Name, im HTML: asp-page-handler="AnalyzeSocial"
     */
    // ── Instagram / TikTok analysieren ──
    public async Task<IActionResult> OnPostAnalyzeSocialAsync()
    {
        LoadCategories();
        ActiveTab = "social"; // Social-Tab aktiv halten

        // Pflichtprüfung: Caption (der Text) muss vorhanden sein
        if (string.IsNullOrWhiteSpace(SocialCaption))
        {
            ErrorMessage = "Bitte füge den Text / die Caption des Posts ein.";
            return Page();
        }

        RecipeData? recipeData;
        try
        {
            /*
             * Wir benutzen denselben GeminiService wie für YouTube.
             * Statt Videotitel und -beschreibung übergeben wir:
             *   - "Social Media Post" als fiktiven "Titel"
             *   - Die Caption als "Beschreibung"
             * Der Prompt im GeminiService funktioniert für beide Quellen.
             */
            // Wir schicken die Caption als "Beschreibung" an die KI
            recipeData = await _gemini.ExtractRecipeAsync("Social Media Post", SocialCaption);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"KI Fehler: {ex.Message}";
            return Page();
        }

        if (recipeData == null)
        {
            ErrorMessage = "KI konnte kein Rezept aus dem Text extrahieren. Ist wirklich ein Rezept im Text?";
            return Page();
        }

        // KI-Daten ins Formular übertragen
        Recipe.Title = recipeData.Title;
        Recipe.Description = recipeData.Description;
        Recipe.Ingredients = recipeData.Ingredients;
        Recipe.Steps = recipeData.Steps;
        Recipe.Equipment = string.IsNullOrWhiteSpace(recipeData.Equipment) ? null : recipeData.Equipment;
        Recipe.Servings = recipeData.Servings > 0 ? recipeData.Servings : 4;

        /*
         * SocialUrl ist optional: Wenn eine URL eingegeben wurde, speichern wir sie.
         * Wenn nicht (leer oder null), setzen wir null.
         *
         * "string.IsNullOrWhiteSpace(...) ? null : SocialUrl"
         * ist ein ternärer Operator: Bedingung ? Wenn-wahr : Wenn-falsch
         */
        Recipe.VideoUrl = string.IsNullOrWhiteSpace(SocialUrl) ? null : SocialUrl;

        RecipeLoaded = true;
        return Page();
    }

    /*
     * ── OnPostSaveAsync() – Rezept speichern ──
     *
     * Wird aufgerufen wenn der Benutzer auf "Rezept speichern" klickt.
     * Das Formular wurde vorausgefüllt (von KI oder manuell) und der Benutzer
     * hat die Daten geprüft/bearbeitet und bestätigt.
     *
     * "Save" = Handler-Name, im HTML: asp-page-handler="Save"
     */
    // ── Rezept speichern (für beide Tabs) ──
    public async Task<IActionResult> OnPostSaveAsync()
    {
        /*
         * Leere URL-Felder auf null setzen.
         * Warum? Das [Url]-Attribut auf dem Recipe-Model würde einen leeren String ""
         * als ungültige URL markieren und die Validierung scheitern lassen.
         * null ist kein URL-Problem (null = kein Wert), "" wäre ein Problem.
         *
         * string.IsNullOrWhiteSpace("") = true → setze auf null
         * string.IsNullOrWhiteSpace("https://...") = false → behalte den Wert
         */
        // Leere URLs auf null setzen (damit [Url]-Validierung nicht stört)
        if (string.IsNullOrWhiteSpace(Recipe.ImageUrl)) Recipe.ImageUrl = null;
        if (string.IsNullOrWhiteSpace(Recipe.VideoUrl)) Recipe.VideoUrl = null;

        /*
         * Manuelle Validierung der Pflichtfelder.
         * Wir prüfen nur was wirklich Pflicht ist.
         * (ModelState.IsValid wird hier bewusst nicht benutzt, da wir
         * mehr Kontrolle über die Fehlermeldungen haben wollen.)
         *
         * Bei jedem Fehler:
         *   1. Fehlermeldung setzen
         *   2. Kategorien neu laden (für das Dropdown)
         *   3. RecipeLoaded = true (Formular wieder anzeigen)
         *   4. return Page() (Seite mit Fehler anzeigen)
         */
        // Nur manuell prüfen was wirklich Pflicht ist
        if (string.IsNullOrWhiteSpace(Recipe.Title))
        {
            ErrorMessage = "Titel ist erforderlich.";
            LoadCategories();
            RecipeLoaded = true;
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Recipe.Ingredients))
        {
            ErrorMessage = "Zutaten sind erforderlich.";
            LoadCategories();
            RecipeLoaded = true;
            return Page();
        }
        if (string.IsNullOrWhiteSpace(Recipe.Steps))
        {
            ErrorMessage = "Zubereitung ist erforderlich.";
            LoadCategories();
            RecipeLoaded = true;
            return Page();
        }

        /*
         * Rezept in der Datenbank speichern:
         *   1. CreatedAt = jetzt (Erstellungszeitpunkt setzen)
         *   2. _db.Recipes.Add(Recipe) = Rezept zur Datenbank hinzufügen (noch nicht gespeichert!)
         *   3. await _db.SaveChangesAsync() = Änderungen wirklich in DB schreiben (SQL INSERT)
         *
         * Nach SaveChangesAsync() hat Recipe.Id automatisch einen Wert (von der Datenbank vergeben).
         */
        Recipe.CreatedAt = DateTime.Now;
        _db.Recipes.Add(Recipe);          // Rezept zum Tracking hinzufügen
        await _db.SaveChangesAsync();     // SQL INSERT ausführen, ID wird gesetzt

        /*
         * Nach dem Speichern zur Detailseite weiterleiten.
         * RedirectToPage leitet den Browser um – der Benutzer sieht die neue URL.
         * new { id = Recipe.Id } übergibt die ID als URL-Parameter: /Recipes/Detail/42
         *
         * Warum weiterleiten statt Page() zurückgeben?
         * Das sogenannte "POST-Redirect-GET-Pattern" verhindert, dass beim Aktualisieren
         * der Seite das Formular nochmal abgeschickt wird.
         */
        return RedirectToPage("/Recipes/Detail", new { id = Recipe.Id });
    }

    /*
     * ── HILFSMETHODE: LoadCategories() ──
     *
     * "private" = nur innerhalb von ImportModel aufrufbar.
     * Diese Methode lädt alle Kategorien aus der Datenbank
     * und bereitet sie als SelectList für das Dropdown-Menü auf.
     *
     * SelectList-Parameter:
     *   - Die Liste der Objekte (alle Categories alphabetisch sortiert)
     *   - "Id"   = Welche Eigenschaft als Wert im <option value="..."> verwendet wird
     *   - "Name" = Welche Eigenschaft als Text im <option>...</option> angezeigt wird
     *
     * .OrderBy(c => c.Name) sortiert alphabetisch nach dem Namen.
     * .ToList() führt die Datenbankabfrage aus und gibt eine List<Category> zurück.
     *   (Ohne .ToList() würde die Abfrage nur "geplant" sein, nicht ausgeführt.)
     */
    private void LoadCategories()
    {
        CategoryOptions = new SelectList(
            _db.Categories.OrderBy(c => c.Name).ToList(),
            "Id", "Name"
        );
    }
}
