/*
 * ============================================================
 * DETAIL.CSHTML.CS – PageModel für die Rezept-Detailseite
 * ============================================================
 *
 * Diese Seite zeigt ein einzelnes Rezept vollständig an.
 * URL-Format: /Recipes/Detail?id=42 (wobei 42 die Rezept-ID ist)
 *
 * Mögliche Aktionen auf dieser Seite:
 *   1. Rezept anzeigen (GET-Request mit ID)
 *   2. Favorit-Status umschalten (POST: Herz-Button)
 *   3. Rezept löschen (POST: Löschen-Button)
 *
 * Das ist ein gutes Beispiel für eine einfache CRUD-Seite:
 *   C = Create (hier nicht, das ist die Import-Seite)
 *   R = Read   (OnGetAsync – Rezept lesen und anzeigen)
 *   U = Update (OnPostToggleFavoriteAsync – Favorit umschalten)
 *   D = Delete (OnPostDeleteAsync – Rezept löschen)
 */

// Bibliotheken einbinden
using Microsoft.AspNetCore.Mvc;                     // IActionResult, NotFound, RedirectToPage usw.
using Microsoft.AspNetCore.Mvc.RazorPages;          // PageModel (Basisklasse)
using Microsoft.EntityFrameworkCore;                // Include(), FirstOrDefaultAsync() usw.
using rezepte.Data;                                  // AppDbContext
using rezepte.Models;                                // Recipe, Category

// Namespace: Diese Datei gehört zu Pages → Recipes
namespace rezepte.Pages.Recipes;

/*
 * DetailModel erbt von PageModel.
 * "Detail" passt zur Datei Detail.cshtml – Razor Pages verknüpft das automatisch.
 */
public class DetailModel : PageModel
{
    /*
     * _db = Datenbankzugriff per Dependency Injection.
     * Wird im Konstruktor übergeben und unveränderlich gespeichert.
     */
    private readonly AppDbContext _db;

    /*
     * ── KONSTRUKTOR ──
     *
     * ASP.NET übergibt den AppDbContext automatisch per DI.
     * Registriert in Program.cs: builder.Services.AddDbContext<AppDbContext>(...)
     */
    public DetailModel(AppDbContext db)
    {
        _db = db;
    }

    /*
     * Recipe – Das Rezept das auf der Seite angezeigt wird.
     *
     * "null!" = der Compiler warnt normalerweise "das könnte null sein",
     * aber wir sagen ihm "vertrau mir, vor der Anzeige wird das gesetzt".
     * In OnGetAsync() setzen wir Recipe = recipe, bevor die Seite angezeigt wird.
     * Wenn das Rezept nicht existiert, geben wir NotFound() zurück statt die Seite zu zeigen.
     *
     * Kein [BindProperty] hier, weil Recipe durch Datenbankabfrage geladen wird,
     * nicht aus einem Formular.
     */
    public Recipe Recipe { get; set; } = null!;

    /*
     * ── OnGetAsync(int id) – Rezept laden und anzeigen ──
     *
     * Wird aufgerufen wenn die Seite aufgerufen wird: /Recipes/Detail?id=42
     * "int id" = der id-Parameter wird aus der URL gelesen.
     *
     * "async Task<IActionResult>" = asynchrone Methode, gibt IActionResult zurück.
     * (Im Gegensatz zu OnGet() das void zurückgibt – hier brauchen wir NotFound().)
     */
    public async Task<IActionResult> OnGetAsync(int id)
    {
        /*
         * _db.Recipes = Zugriff auf die Recipes-Tabelle in der Datenbank.
         *
         * .Include(r => r.Category):
         *   - Lädt auch die zugehörige Kategorie mit (JOIN in SQL)
         *   - Ohne .Include() wäre recipe.Category == null (nicht geladen)
         *   - Mit .Include() können wir im Template recipe.Category.Name benutzen
         *   - "r => r.Category" ist ein Lambda: für jedes Rezept r, lade auch r.Category
         *
         * .FirstOrDefaultAsync(r => r.Id == id):
         *   - Sucht das erste Rezept bei dem r.Id == id ist
         *   - "OrDefault" = wenn nichts gefunden wird, gib null zurück (kein Fehler!)
         *   - Ohne "OrDefault" würde eine Exception geworfen wenn das Rezept nicht existiert
         *   - "Async" = asynchron (await davor = wartet ohne zu blockieren)
         *
         * Ergebnis: ein Recipe-Objekt mit geladener Kategorie, oder null
         */
        var recipe = await _db.Recipes
            .Include(r => r.Categories)              // Alle Kategorien mitlesen (JOIN)
            .FirstOrDefaultAsync(r => r.Id == id);  // Erstes Rezept mit dieser ID

        /*
         * null-Check: Wenn kein Rezept gefunden wurde (ungültige ID, gelöscht usw.)
         * → HTTP 404 Not Found zurückgeben.
         *
         * "return NotFound()" bricht die Methode sofort ab.
         * Das Template wird NICHT angezeigt (kein Rezept zum Anzeigen).
         */
        if (recipe == null) return NotFound();

        // Gefundenes Rezept in die öffentliche Property speichern (für das Template)
        Recipe = recipe;

        /*
         * Page() = "Zeige das Template dieser Seite an."
         * Das Template (Detail.cshtml) kann jetzt auf @Model.Recipe zugreifen.
         */
        return Page();
    }

    /*
     * ── OnPostToggleFavoriteAsync(int id) – Favorit umschalten ──
     *
     * Wird aufgerufen wenn der Benutzer auf den Herz/Favorit-Button klickt.
     * "ToggleFavorite" = Handler-Name, im HTML: asp-page-handler="ToggleFavorite"
     *
     * "Toggle" bedeutet umschalten: true → false, false → true.
     * Das Rezept wird entweder als Favorit markiert oder die Markierung entfernt.
     */
    public async Task<IActionResult> OnPostToggleFavoriteAsync(int id)
    {
        /*
         * FindAsync(id) = Sucht einen Datensatz über den Primärschlüssel.
         * Schneller als FirstOrDefaultAsync weil EF direkt nach ID sucht (Index-Lookup).
         * ABER: FindAsync lädt keine Navigation Properties (kein .Include() möglich).
         * Hier ist das ok, wir brauchen nur IsFavorite zu ändern.
         *
         * "await" wartet asynchron auf das Ergebnis.
         */
        var recipe = await _db.Recipes.FindAsync(id);

        // null-Check: Rezept existiert nicht → 404 zurückgeben
        if (recipe == null) return NotFound();

        /*
         * Favorit-Status umschalten (Toggle):
         * recipe.IsFavorite = !recipe.IsFavorite
         *   - Wenn true  → wird false (war Favorit, jetzt nicht mehr)
         *   - Wenn false → wird true  (war kein Favorit, jetzt schon)
         * "!" ist der Negationsoperator in C#.
         *
         * Danach speichern:
         * await _db.SaveChangesAsync() = SQL UPDATE ausführen
         *   (EF erkennt automatisch was sich geändert hat und aktualisiert nur das)
         */
        recipe.IsFavorite = !recipe.IsFavorite;
        await _db.SaveChangesAsync();

        /*
         * Nach dem Speichern zur Detailseite zurückleiten.
         * "new { id }" = Kurzschreibweise für new { id = id } (Variable-Name = Property-Name)
         * Das ist das POST-Redirect-GET-Pattern:
         *   → POST (Daten ändern) → Redirect → GET (Seite anzeigen)
         * Verhindert, dass beim Browser-Aktualisieren das Formular nochmal abgeschickt wird.
         */
        return RedirectToPage(new { id });
    }

    /*
     * ── OnPostDeleteAsync(int id) – Rezept löschen ──
     *
     * Wird aufgerufen wenn der Benutzer auf "Rezept löschen" klickt.
     * "Delete" = Handler-Name, im HTML: asp-page-handler="Delete"
     *
     * Nach dem Löschen leiten wir zur Startseite (/Index) weiter,
     * weil die Detailseite dieses Rezepts ja nicht mehr existiert.
     */
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        /*
         * Rezept in der Datenbank suchen.
         * FindAsync ist schnell für Primärschlüssel-Suche.
         */
        var recipe = await _db.Recipes.FindAsync(id);

        /*
         * Nur löschen wenn das Rezept gefunden wurde.
         * "if (recipe != null)" = wenn das Rezept existiert.
         * Wenn es nicht existiert (null), überspringen wir das Löschen.
         * Das verhindert Fehler wenn jemand denselben Link zweimal klickt.
         *
         * Innen:
         *   _db.Recipes.Remove(recipe) = Datensatz zum Löschen markieren
         *   await _db.SaveChangesAsync() = SQL DELETE ausführen
         */
        if (recipe != null)
        {
            _db.Recipes.Remove(recipe);      // Rezept aus dem Tracking entfernen (markieren)
            await _db.SaveChangesAsync();    // SQL DELETE an die Datenbank schicken
        }

        /*
         * Nach dem Löschen zur Startseite weiterleiten.
         * "/Index" = die Hauptseite des Projekts (Pages/Index.cshtml).
         * Das Rezept existiert nicht mehr, also kann man nicht auf der Detailseite bleiben.
         *
         * Kein "new { id }" hier – auf der Index-Seite ist keine ID nötig.
         */
        return RedirectToPage("/Index");
    }
}
