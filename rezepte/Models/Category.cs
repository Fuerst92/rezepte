/*
 * ============================================================
 * CATEGORY.CS – Das Datenmodell für eine Kategorie
 * ============================================================
 *
 * Was ist eine Kategorie in diesem Projekt?
 * Eine Kategorie gruppiert Rezepte, zum Beispiel:
 *   - "Italienisch", "Vegan", "Desserts", "Schnelle Küche"
 *
 * Dieses Model ist einfacher als Recipe.cs.
 * Es hat nur wenige Eigenschaften, zeigt aber gut das Konzept
 * einer "Eins-zu-viele-Beziehung" in Datenbanken:
 *   → Eine Kategorie kann viele Rezepte enthalten
 *   → Ein Rezept gehört zu genau einer Kategorie
 *
 * In der Datenbank gibt es dafür zwei Tabellen:
 *   - Tabelle "Categories" (diese Klasse)
 *   - Tabelle "Recipes" (Recipe.cs) mit einer CategoryId-Spalte
 *
 * Die Verbindung: Categories.Id == Recipes.CategoryId
 */

// Bibliothek für Validierungs-Attribute wie [Required], [StringLength], [Display]
using System.ComponentModel.DataAnnotations;

// Namespace: Diese Klasse gehört zum Bereich "Models" im Projekt "rezepte"
namespace rezepte.Models;

// "public class Category" = öffentlich zugängliche Klasse namens Category
public class Category
{
    /*
     * Id – Primärschlüssel der Kategorien-Tabelle.
     * Entity Framework erkennt "Id" automatisch als Primärschlüssel.
     * Die Datenbank vergibt automatisch aufsteigende Nummern: 1, 2, 3, ...
     * Man muss den Wert nie selbst setzen – die Datenbank macht das.
     */
    public int Id { get; set; }

    /*
     * [Required] – Name ist Pflichtfeld.
     * Eine Kategorie ohne Namen macht keinen Sinn.
     * Wenn das Formular ohne Namen abgeschickt wird, zeigt das Framework einen Fehler.
     *
     * [StringLength(50)] – Maximal 50 Zeichen.
     * Kurze, prägnante Kategorienamen reichen aus.
     * In der Datenbank wird die Spalte entsprechend begrenzt.
     *
     * [Display(Name = "Kategorie")] – Anzeigename in Formularen und Fehlermeldungen.
     * Ohne [Display] würde das Framework "Name" als Label-Text benutzen.
     *
     * = string.Empty – Standardwert ist ein leerer String (nie null).
     */
    [Required]
    [StringLength(50)]
    [Display(Name = "Kategorie")]
    public string Name { get; set; } = string.Empty;

    /*
     * Color – Die Farbe der Kategorie (als HTML-Farbcode).
     * Farbcodes in HTML sehen so aus: "#6c757d" (Grau), "#ff0000" (Rot), usw.
     * Das "#" ist immer dabei, danach kommen 6 Hexadezimalziffern.
     *
     * [StringLength(20)] – 20 Zeichen reichen für Farbcodes (#rrggbb = 7 Zeichen).
     *
     * = "#6c757d" – Standardfarbe ist ein mittleres Grau (Bootstrap-Grauton).
     * Das bedeutet: Wenn eine Kategorie ohne Farbe erstellt wird, ist sie automatisch grau.
     *
     * Kein [Required] hier, da ein Standardwert vorhanden ist.
     */
    [StringLength(20)]
    public string Color { get; set; } = "#6c757d";

    /*
     * Recipes – Die Liste aller Rezepte, die zu dieser Kategorie gehören.
     *
     * Das ist die "Navigation Property" für die Eins-zu-viele-Beziehung.
     * Entity Framework kann automatisch alle Rezepte einer Kategorie laden:
     *   category.Recipes → gibt alle Rezepte dieser Kategorie zurück
     *
     * List<Recipe> ist eine generische Liste von Recipe-Objekten.
     *   <Recipe> bedeutet: Diese Liste enthält nur Recipe-Objekte (nicht gemischt).
     *   List ist eine dynamisch wachsende Sammlung (kein festes Array).
     *
     * = new() ist Kurzschreibweise für = new List<Recipe>()
     *   (C# kann den Typ aus der Deklaration ableiten)
     *   Damit ist die Liste beim Start leer, aber nie null –
     *   das verhindert NullReferenceException-Fehler.
     *
     * WICHTIG: Diese Liste wird von Entity Framework nur gefüllt, wenn man
     * .Include(c => c.Recipes) in der Datenbankabfrage angibt.
     * Ohne .Include() ist die Liste immer leer, auch wenn Rezepte in der DB sind.
     */
    public List<Recipe> Recipes { get; set; } = new();
}
