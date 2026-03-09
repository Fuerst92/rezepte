/*
 * ============================================================
 * CATEGORIES/INDEX.CSHTML.CS – PageModel für die Kategorien-Verwaltung
 * ============================================================
 *
 * Diese Seite zeigt alle Kategorien an und ermöglicht:
 *   1. Neue Kategorien erstellen (mit Name und Farbe)
 *   2. Bestehende Kategorien löschen (nur wenn keine Rezepte darin)
 *
 * Die Seite ist erreichbar unter: /Categories/Index (oder /Categories/)
 *
 * Sie demonstriert gut das Grundprinzip von Razor Pages:
 *   - OnGet()       = Seite anzeigen (Daten laden)
 *   - OnPostAdd()   = Formular für "Hinzufügen" wurde abgeschickt
 *   - OnPostDelete()= Formular für "Löschen" wurde abgeschickt
 *
 * Jeder Button im HTML hat einen Handler-Namen:
 *   <button asp-page-handler="Add">    → ruft OnPostAdd() auf
 *   <button asp-page-handler="Delete"> → ruft OnPostDelete() auf
 */

// Bibliotheken einbinden
using Microsoft.AspNetCore.Mvc;           // IActionResult, NotFound() usw.
using Microsoft.AspNetCore.Mvc.RazorPages; // PageModel
using rezepte.Data;                        // AppDbContext (Datenbankzugriff)
using rezepte.Models;                      // Category, Recipe (Datenmodelle)

// Namespace entspricht dem Ordnerpfad: Pages → Categories
namespace rezepte.Pages.Categories;

/*
 * IndexModel erbt von PageModel.
 * "Index" ist der Standard-Name für die Hauptseite eines Ordners.
 * /Categories/ → lädt automatisch Pages/Categories/Index.cshtml
 */
public class IndexModel : PageModel
{
    /*
     * _db = Datenbankzugriff (Dependency Injection).
     * "private readonly" = unveränderlich nach Zuweisung im Konstruktor.
     *
     * AppDbContext ist wie eine Verbindung zur Datenbank – über ihn können wir
     * Kategorien und Rezepte lesen, hinzufügen und löschen.
     */
    private readonly AppDbContext _db;

    /*
     * ── KONSTRUKTOR ──
     *
     * ASP.NET übergibt automatisch den AppDbContext per Dependency Injection.
     * Voraussetzung: In Program.cs wurde AddDbContext<AppDbContext>() aufgerufen.
     */
    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    /*
     * Categories – Die Liste aller Kategorien, die auf der Seite angezeigt werden.
     * List<Category> = Liste von Category-Objekten.
     * "= new()" = leere Liste als Standardwert (nie null).
     *
     * Diese Property wird in LoadCategories() befüllt und im HTML-Template
     * mit @Model.Categories durchlaufen (foreach-Schleife im HTML).
     *
     * Kein [BindProperty] hier, weil diese Liste nicht aus einem Formular kommt –
     * sie wird aus der Datenbank geladen.
     */
    public List<Category> Categories { get; set; } = new();

    /*
     * [BindProperty] = wird automatisch aus dem Formularfeld "NewCategoryName" befüllt.
     * NewCategoryName = Name der neuen Kategorie (aus dem Eingabefeld).
     *
     * Standardwert "" (leerer String), nicht null.
     */
    [BindProperty]
    public string NewCategoryName { get; set; } = "";

    /*
     * [BindProperty] = wird aus dem Formularfeld "NewCategoryColor" befüllt.
     * NewCategoryColor = Farbe der neuen Kategorie als HTML-Farbcode.
     * Standardwert "#6c757d" = mittleres Grau (Bootstrap-Standardfarbe).
     *
     * Im HTML gibt es dafür ein <input type="color"> – ein Farbauswahlfeld.
     */
    [BindProperty]
    public string NewCategoryColor { get; set; } = "#6c757d";

    // Felder für das Bearbeiten-Formular
    [BindProperty]
    public int EditCategoryId { get; set; }
    [BindProperty]
    public string EditCategoryName { get; set; } = "";
    [BindProperty]
    public string EditCategoryColor { get; set; } = "#6c757d";

    /*
     * Statusmeldungen für den Benutzer:
     *   ErrorMessage   = Fehlermeldung (z.B. "Kategorie existiert bereits")
     *   SuccessMessage = Erfolgsmeldung (z.B. "Kategorie wurde erstellt!")
     *
     * "string?" = kann null sein (kein Fehler/Erfolg → null → keine Meldung im HTML).
     */
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    /*
     * ── OnGet() ──
     *
     * Wird bei jedem normalen Seitenaufruf (GET-Request) aufgerufen.
     * Lädt alle Kategorien aus der Datenbank und stellt sie dem Template bereit.
     *
     * "void" = kein Rückgabewert (die Seite wird immer angezeigt, keine Weiterleitung).
     */
    public void OnGet()
    {
        LoadCategories(); // Kategorien aus DB laden
    }

    /*
     * ── OnPostAdd() – Neue Kategorie hinzufügen ──
     *
     * Wird aufgerufen wenn das Formular "Neue Kategorie" abgeschickt wird.
     * "IActionResult" = kann Page() (Seite zeigen) oder Redirect zurückgeben.
     *
     * Wichtig: Diese Methode ist NICHT async, weil alle Datenbankoperationen
     * hier synchron sind (SaveChanges() statt SaveChangesAsync()).
     * Das ist bei einfachen Operationen akzeptabel.
     */
    // Neue Kategorie hinzufügen
    public IActionResult OnPostAdd()
    {
        /*
         * Validierung 1: Name darf nicht leer sein.
         * string.IsNullOrWhiteSpace("") = true → kein gültiger Name.
         * Wir setzen die Fehlermeldung, laden Kategorien neu und zeigen die Seite.
         */
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            ErrorMessage = "Bitte einen Namen eingeben.";
            LoadCategories();
            return Page();
        }

        /*
         * Validierung 2: Kategoriename darf noch nicht existieren (keine Duplikate).
         *
         * _db.Categories.Any(c => c.Name == NewCategoryName.Trim())
         *   - _db.Categories = die Categories-Tabelle in der Datenbank
         *   - .Any(...) = gibt true zurück wenn MINDESTENS EIN Datensatz die Bedingung erfüllt
         *   - c => c.Name == NewCategoryName.Trim() = Lambda-Ausdruck (Filterbedingung)
         *     → für jeden Datensatz c: ist der Name gleich dem neuen Namen?
         *   - .Trim() entfernt Leerzeichen am Anfang/Ende (sicherheitshalber)
         *
         * Das ist ein einfaches LINQ (Language Integrated Query) – SQL-ähnliche Abfragen in C#.
         * Entity Framework übersetzt .Any() automatisch in ein SQL: SELECT 1 WHERE ...
         *
         * $"..." ist String-Interpolation: die Variable wird direkt in den String eingebaut.
         * \" ist ein escaped Anführungszeichen innerhalb des Strings.
         */
        if (_db.Categories.Any(c => c.Name == NewCategoryName.Trim()))
        {
            ErrorMessage = $"Die Kategorie \"{NewCategoryName}\" existiert bereits.";
            LoadCategories();
            return Page();
        }

        /*
         * Neue Kategorie erstellen und speichern:
         *   1. new Category { Name = ..., Color = ... } = neues Objekt erstellen
         *   2. .Trim() = Leerzeichen am Anfang/Ende entfernen
         *   3. _db.Categories.Add(...) = zum DatenBank-Tracking hinzufügen
         *   4. _db.SaveChanges() = SQL INSERT ausführen (synchron)
         *
         * "Object Initializer" { Name = ..., Color = ... }:
         * Das ist eine Kurzschreibweise um ein Objekt zu erstellen UND Werte zu setzen.
         * Statt: var c = new Category(); c.Name = ...; c.Color = ...;
         * Einfach: new Category { Name = ..., Color = ... }
         */
        _db.Categories.Add(new Category { Name = NewCategoryName.Trim(), Color = NewCategoryColor });
        _db.SaveChanges();

        // Erfolgsmeldung setzen
        SuccessMessage = $"Kategorie \"{NewCategoryName.Trim()}\" wurde erstellt!";
        LoadCategories(); // Liste aktualisieren (neue Kategorie soll erscheinen)
        return Page();    // Seite mit Erfolgsmeldung anzeigen
    }

    /*
     * ── OnPostDelete(int id) – Kategorie löschen ──
     *
     * Wird aufgerufen wenn der "Löschen"-Button einer Kategorie geklickt wird.
     * "int id" = der Parameter kommt aus dem HTML-Formular (Route oder verstecktes Feld).
     *
     * Im HTML gibt es für jede Kategorie einen Button mit der ID:
     *   <button asp-page-handler="Delete" asp-route-id="@category.Id">
     * ASP.NET liest die ID aus der Route und übergibt sie hier als Parameter.
     *
     * Sicherheitscheck: Wir prüfen ob noch Rezepte in dieser Kategorie sind.
     * Eine Kategorie mit Rezepten zu löschen würde diese "verwaisen" lassen.
     */
    // Kategorie bearbeiten
    public IActionResult OnPostEdit()
    {
        if (string.IsNullOrWhiteSpace(EditCategoryName))
        {
            ErrorMessage = "Bitte einen Namen eingeben.";
            LoadCategories();
            return Page();
        }

        var category = _db.Categories.Find(EditCategoryId);
        if (category == null) return NotFound();

        // Prüfen ob der neue Name schon von einer ANDEREN Kategorie verwendet wird
        if (_db.Categories.Any(c => c.Name == EditCategoryName.Trim() && c.Id != EditCategoryId))
        {
            ErrorMessage = $"Die Kategorie \"{EditCategoryName}\" existiert bereits.";
            LoadCategories();
            return Page();
        }

        category.Name  = EditCategoryName.Trim();
        category.Color = EditCategoryColor;
        _db.SaveChanges();

        SuccessMessage = $"Kategorie \"{category.Name}\" wurde aktualisiert!";
        LoadCategories();
        return Page();
    }

    // Kategorie löschen
    public IActionResult OnPostDelete(int id)
    {
        /*
         * _db.Categories.Find(id) sucht einen Datensatz mit dem Primärschlüssel id.
         * Gibt null zurück wenn kein Datensatz mit dieser ID gefunden wird.
         *
         * NotFound() = gibt HTTP 404 zurück.
         * Das passiert wenn jemand manipuliert und eine ungültige ID schickt.
         * Das "return" bricht die Methode sofort ab.
         */
        var category = _db.Categories.Find(id);
        if (category == null) return NotFound();

        /*
         * Prüfen ob noch Rezepte in dieser Kategorie sind.
         * _db.Recipes.Any(r => r.CategoryId == id)
         *   → true, wenn mindestens ein Rezept diese Kategorie hat
         *
         * Wenn ja: Fehlermeldung und Abbruch (Kategorie NICHT löschen).
         * Das schützt die referentielle Integrität der Datenbank:
         * Kein Rezept soll auf eine nicht-existente Kategorie zeigen.
         */
        // Prüfen ob noch Rezepte in dieser Kategorie sind
        if (_db.Recipes.Any(r => r.CategoryId == id))
        {
            ErrorMessage = $"Kategorie \"{category.Name}\" kann nicht gelöscht werden – es gibt noch Rezepte darin.";
            LoadCategories();
            return Page();
        }

        /*
         * Kategorie löschen:
         *   1. _db.Categories.Remove(category) = Datensatz zum Löschen markieren
         *   2. _db.SaveChanges() = SQL DELETE ausführen
         *
         * Nach SaveChanges() ist der Datensatz aus der Datenbank entfernt.
         */
        _db.Categories.Remove(category);
        _db.SaveChanges();

        SuccessMessage = $"Kategorie \"{category.Name}\" wurde gelöscht.";
        LoadCategories(); // Liste aktualisieren (gelöschte Kategorie soll verschwinden)
        return Page();
    }

    /*
     * ── HILFSMETHODE: LoadCategories() ──
     *
     * Lädt alle Kategorien alphabetisch aus der Datenbank
     * und speichert sie in der Categories-Property.
     *
     * "private" = nur innerhalb dieser Klasse aufrufbar.
     * Wird in OnGet(), OnPostAdd() und OnPostDelete() aufgerufen.
     *
     * _db.Categories       = Zugriff auf die Categories-Tabelle
     * .OrderBy(c => c.Name) = alphabetisch nach Name sortieren (LINQ → SQL ORDER BY)
     * .ToList()             = Abfrage ausführen und in eine C#-Liste umwandeln
     */
    private void LoadCategories()
    {
        Categories = _db.Categories
            .OrderBy(c => c.Name) // Alphabetisch sortieren
            .ToList();            // SQL ausführen und Ergebnis als Liste zurückgeben
    }
}
