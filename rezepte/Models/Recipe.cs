/*
 * ============================================================
 * RECIPE.CS – Das Datenmodell für ein Rezept
 * ============================================================
 *
 * Was ist ein "Model" (Datenmodell)?
 * ────────────────────────────────────
 * Ein Model ist eine Klasse, die eine "Sache aus der echten Welt" beschreibt.
 * In unserem Fall: Ein Rezept hat einen Titel, eine Beschreibung, Zutaten, usw.
 *
 * In ASP.NET mit Entity Framework Core hat das Model noch eine zweite Aufgabe:
 * Es bestimmt wie die Datenbanktabelle aussieht!
 * Jede Eigenschaft (Property) dieser Klasse wird zu einer Spalte in der Tabelle "Recipes".
 * Jedes Objekt der Klasse (eine Instanz) wird zu einer Zeile (einem Datensatz) in der Tabelle.
 *
 * Beispiel: Recipe.Title → Spalte "Title" in der Tabelle
 *           Neues Recipe-Objekt speichern → Neue Zeile in der Tabelle
 *
 * Das nennt man "ORM" (Object-Relational Mapping) – Objekte und Datenbank werden verknüpft.
 */

/*
 * System.ComponentModel.DataAnnotations ist eine Bibliothek für sogenannte "Attribute".
 * Attribute sind Anweisungen in eckigen Klammern [Wie Diese], die dem Framework
 * zusätzliche Informationen über eine Eigenschaft geben.
 * Zum Beispiel: [Required] sagt "dieses Feld muss ausgefüllt sein".
 */
using System.ComponentModel.DataAnnotations;

/*
 * "namespace" ist wie ein Ordner für Code.
 * Er verhindert Namenskonflikte: Wenn zwei Klassen "Recipe" heißen, aber in
 * verschiedenen Namespaces sind, weiß der Compiler welche gemeint ist.
 * "rezepte.Models" bedeutet: Diese Klasse gehört zum Projekt "rezepte", Unterbereich "Models".
 */
namespace rezepte.Models;

/*
 * "public class Recipe" definiert eine neue Klasse namens "Recipe".
 * "public" = diese Klasse ist von überall erreichbar (auch aus anderen Dateien/Namespaces).
 * Eine Klasse ist wie eine Vorlage (Bauplan) für Objekte.
 * Ein konkretes Rezept (z.B. "Spaghetti Bolognese") ist dann ein Objekt dieser Klasse.
 */
public class Recipe
{
    /*
     * Id ist der Primärschlüssel der Datenbanktabelle.
     * Entity Framework erkennt automatisch: eine Eigenschaft namens "Id" (oder "[Klassenname]Id")
     * wird zum Primärschlüssel. Das bedeutet:
     *   - Jeder Datensatz bekommt eine eindeutige Nummer
     *   - Die Datenbank vergibt die Nummer automatisch (auto-increment)
     *   - Damit kann man jeden Datensatz eindeutig identifizieren
     *
     * { get; set; } ist eine "auto-implementierte Property":
     *   - get = Wert lesen (z.B. recipe.Id gibt 42 zurück)
     *   - set = Wert schreiben (z.B. recipe.Id = 42)
     *   - Intern erstellt C# automatisch eine private Variable dafür
     */
    public int Id { get; set; }

    /*
     * [Required] – Validierungs-Attribut
     * Sagt dem Framework: Dieses Feld MUSS ausgefüllt sein.
     * Wenn ein Formular ohne Titel abgeschickt wird, zeigt das Framework
     * automatisch die ErrorMessage an und speichert das Rezept NICHT.
     * Ohne [Required] könnte man Rezepte ohne Titel speichern.
     *
     * [StringLength(100)] – Maximale Länge begrenzen
     * Der Titel darf maximal 100 Zeichen lang sein.
     * Wichtig sowohl für die Datenbank als auch für die UI-Validierung.
     *
     * [Display(Name = "Titel")] – Anzeigename für Formulare
     * Wenn das Framework automatisch Labels generiert, benutzt es diesen Namen.
     * Ohne [Display] würde es den Eigenschaftsnamen "Title" nehmen.
     *
     * "string.Empty" = leerer String ""
     * "= string.Empty" setzt den Standardwert auf einen leeren String,
     * damit die Eigenschaft nie null ist (was Null-Fehler verhindern hilft).
     */
    [Required(ErrorMessage = "Titel ist erforderlich")]
    [StringLength(100, ErrorMessage = "Maximal 100 Zeichen")]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    /*
     * Beschreibung ist NICHT required ([Required] fehlt).
     * Das bedeutet, dieses Feld ist optional – man kann ein Rezept auch ohne
     * Beschreibung speichern.
     * StringLength(2000) erlaubt bis zu 2000 Zeichen (für längere Texte als der Titel).
     */
    [Display(Name = "Beschreibung")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /*
     * Zutaten sind wieder Pflicht ([Required]).
     * Zutaten werden als einzelner langer Text gespeichert
     * (z.B. "200g Mehl\n3 Eier\n100ml Milch" – Zeilen mit \n getrennt).
     * Kein StringLength hier → keine Längenbeschränkung in der Datenbank.
     */
    [Required(ErrorMessage = "Zutaten sind erforderlich")]
    [Display(Name = "Zutaten")]
    public string Ingredients { get; set; } = string.Empty;

    /*
     * Zubereitungsschritte, ebenfalls Pflicht.
     * Auch hier: Text mit Zeilenumbrüchen für die einzelnen Schritte.
     */
    [Required(ErrorMessage = "Zubereitung ist erforderlich")]
    [Display(Name = "Zubereitung")]
    public string Steps { get; set; } = string.Empty;

    /*
     * ImageUrl (Bild-URL) ist ein "nullable string" – erkennbar am "?" nach "string".
     * "string?" bedeutet: dieser Wert DARF null sein (kein Bild = null).
     * Ohne "?" könnte der Wert nie null sein, was für optionale Felder ungünstig ist.
     *
     * [Url] prüft ob der eingegebene Text eine gültige URL ist (beginnt mit http:// etc.).
     * Wenn jemand "keinurl" eingibt, zeigt das Framework einen Fehler.
     * Wichtig: [Url] wird nur geprüft wenn der Wert NICHT null/leer ist.
     */
    [Display(Name = "Bild-URL")]
    [Url(ErrorMessage = "Bitte eine gültige URL eingeben")]
    public string? ImageUrl { get; set; }

    /*
     * VideoUrl – Link zum YouTube- oder anderen Video-Link.
     * Ebenfalls optional (string?) und mit URL-Validierung.
     */
    [Display(Name = "Video-Link")]
    [Url(ErrorMessage = "Bitte eine gültige URL eingeben")]
    public string? VideoUrl { get; set; }

    /*
     * IsFavorite – Ist dieses Rezept als Favorit markiert?
     * bool kann nur true (ja) oder false (nein) sein.
     * Standardwert ist false (= kein Favorit beim Erstellen).
     */
    [Display(Name = "Favorit")]
    public bool IsFavorite { get; set; } = false;

    /*
     * Servings – Wie viele Portionen das Rezept ergibt (Basis für den Portionsrechner).
     * Der Portionsrechner auf der Detailseite rechnet die Zutaten relativ zu diesem Wert hoch/runter.
     * Standardwert: 4 Portionen.
     */
    [Display(Name = "Portionen")]
    [Range(1, 100, ErrorMessage = "Portionen müssen zwischen 1 und 100 liegen")]
    public int Servings { get; set; } = 4;

    /*
     * Equipment – Benötigte Küchengeräte und Utensilien (z.B. "Backofen\nSchneidebrett\nMesser").
     * Wird wie Ingredients als Text mit Zeilenumbrüchen gespeichert.
     * Optional – nicht jedes Rezept braucht eine Geräteliste.
     */
    [Display(Name = "Benötigte Utensilien")]
    public string? Equipment { get; set; }

    /*
     * CreatedAt – Datum und Uhrzeit der Erstellung.
     * DateTime.Now gibt den aktuellen Zeitpunkt zurück.
     * Als Standardwert wird also der Zeitpunkt der Objekterstellung gesetzt.
     * Das Framework speichert diesen Wert automatisch in der Datenbank.
     */
    [Display(Name = "Erstellt am")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /*
     * CategoryId – Fremdschlüssel zur Category-Tabelle.
     * Jedes Rezept gehört zu genau einer Kategorie.
     * CategoryId speichert die ID der zugehörigen Kategorie.
     * Entity Framework erkennt automatisch: "CategoryId" + "Category" = Fremdschlüssel-Beziehung.
     *
     * Das nennt man eine "Eins-zu-viele-Beziehung":
     *   - Eine Kategorie kann viele Rezepte haben
     *   - Ein Rezept gehört zu genau einer Kategorie
     *
     * Category (ohne "Id") ist die "Navigation Property":
     *   - Sie erlaubt direkten Zugriff auf das Category-Objekt: recipe.Category.Name
     *   - Entity Framework lädt die verknüpfte Kategorie automatisch (mit .Include())
     *
     * "null!" bedeutet: wir sagen dem Compiler "vertrau mir, das wird nie null sein,
     * auch wenn ich keinen Wert setze". Das "!" unterdrückt die Compiler-Warnung.
     * In der Praxis wird das Category-Objekt immer geladen, bevor es verwendet wird.
     */
    // Legacy-Spalte aus alter Version (NOT NULL in bestehender DB), wird nicht mehr verwendet
    public int CategoryId { get; set; } = 0;

    // Viele-zu-viele: Ein Rezept kann mehrere Kategorien haben
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
