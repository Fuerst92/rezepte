/*
 * ============================================================
 * PROGRAM.CS – Der Startpunkt der gesamten Webanwendung
 * ============================================================
 *
 * Diese Datei ist das "Herz" der App. Sie wird als ERSTES ausgeführt,
 * wenn du die Anwendung startest. Hier wird alles konfiguriert:
 *   - Welche Dienste (Services) sollen verfügbar sein?
 *   - Wie und wo wird die Datenbank gespeichert?
 *   - Welche Seiten/Routen gibt es?
 *
 * Stell dir vor, du baust ein Restaurant:
 *   - builder = der Architekt, der alles plant
 *   - app     = das fertige Restaurant, das offen ist für Gäste
 */

// "using" bedeutet: wir wollen Code aus einer anderen Bibliothek benutzen.
// Ohne "using" müsste man den vollen Namen tippen, z.B.:
// Microsoft.EntityFrameworkCore.DbContextOptionsBuilder statt einfach DbContextOptionsBuilder
using Microsoft.EntityFrameworkCore;  // Für die Datenbankverbindung (EF Core)
using rezepte.Data;                   // Unsere eigene Datenbankklasse (AppDbContext)
using rezepte.Services;               // Unsere eigenen Service-Klassen (YouTube, Gemini)

/*
 * WebApplication.CreateBuilder(args) erstellt einen "Builder" – also einen Baumeister.
 * Der Builder sammelt alle Einstellungen bevor die App startet.
 * "args" sind mögliche Kommandozeilenargumente (z.B. wenn man die App mit extra Parametern startet).
 * Ohne diese Zeile könntest du gar keine ASP.NET Web-App bauen.
 */
var builder = WebApplication.CreateBuilder(args);

/*
 * ── PORT-KONFIGURATION FÜR RAILWAY ──
 *
 * Railway ist ein Cloud-Hosting-Dienst (wie eine Server-Farm im Internet).
 * Railway gibt der App einen PORT (eine Nummer) über eine Umgebungsvariable.
 * Umgebungsvariablen sind Werte, die das Betriebssystem an die App weitergibt –
 * ähnlich wie Einstellungen, die von außen kommen.
 *
 * "?? "8080"" bedeutet: Wenn PORT nicht gesetzt ist, nimm 8080 als Standard.
 * Das ist der "Null-Koaleszenz-Operator" – er gibt den rechten Wert zurück,
 * wenn der linke null ist.
 */
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// Sagt dem Webserver: Höre auf ALLEN Netzwerkschnittstellen (0.0.0.0) auf dem angegebenen Port.
// "0.0.0.0" bedeutet "alle verfügbaren IP-Adressen", nicht nur localhost.
// Ohne diese Zeile würde die App auf Railway nicht erreichbar sein (sie würde nur lokal lauschen).
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

/*
 * ── RAZOR PAGES AKTIVIEREN ──
 *
 * Razor Pages ist ein Framework von Microsoft für Webseiten.
 * Jede Seite in unserem Projekt (z.B. Pages/Recipes/Detail.cshtml) ist eine Razor Page.
 * Ohne AddRazorPages() würde das Framework diese Seiten nicht kennen und ein Fehler wäre die Folge.
 */
builder.Services.AddRazorPages();

/*
 * ── SERVICES REGISTRIEREN (Dependency Injection) ──
 *
 * Was ist Dependency Injection (DI)?
 * ───────────────────────────────────
 * Stell dir vor, deine Klasse braucht einen "Mitarbeiter" (z.B. YouTubeService).
 * Statt selbst zu sagen "ich stelle jetzt einen Mitarbeiter ein", sagst du:
 * "Ich brauche einen Mitarbeiter – bitte jemanden schicken."
 * Das System (der DI-Container) schickt dann automatisch den richtigen Mitarbeiter.
 *
 * Vorteile:
 *   - Du musst nicht selbst "new YouTubeService()" schreiben
 *   - Klassen werden unabhängiger voneinander (leichter testbar)
 *   - Der DI-Container kümmert sich um die Lebenszeit der Objekte
 *
 * AddHttpClient<T>() bedeutet:
 *   - Erstelle einen HttpClient (ein Objekt zum Senden von HTTP-Anfragen)
 *   - Und gib ihn an YouTubeService / GeminiService weiter
 *   - HttpClient ist wie ein Browser, nur für Code: Er kann URLs aufrufen und Daten laden
 */
builder.Services.AddHttpClient<YouTubeService>(); // YouTubeService bekommt einen HttpClient
builder.Services.AddHttpClient<GeminiService>();  // GeminiService bekommt einen eigenen HttpClient

/*
 * ── DATENBANKPFAD BESTIMMEN ──
 *
 * SQLite ist eine einfache Datenbankdatei (eine einzige .db-Datei).
 * Je nachdem ob wir lokal entwickeln oder auf Railway laufen, muss die Datei
 * an einem anderen Ort gespeichert werden.
 *
 * Railway hat ein spezielles Verzeichnis /data/ für dauerhafte Dateien.
 * Ohne /data/ würde die Datenbank bei jedem Neustart gelöscht (Railway setzt Container zurück).
 *
 * RAILWAY_ENVIRONMENT ist eine Umgebungsvariable, die Railway automatisch setzt.
 * Wenn sie existiert (nicht null ist), wissen wir: wir laufen auf Railway.
 */
string dbPath; // Hier speichern wir den Pfad zur Datenbankdatei

if (Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null)
{
    // Wir sind auf Railway → Datenbank in /data/ speichern (dauerhafter Speicher)
    Directory.CreateDirectory("/data"); // Ordner erstellen falls er nicht existiert
    dbPath = "/data/rezepte.db";        // Pfad auf dem Railway-Server
}
else
{
    // Wir sind lokal → Datenbank im Projektordner speichern (einfacher für Entwicklung)
    dbPath = "rezepte.db";
}

/*
 * ── DATENBANKVERBINDUNG KONFIGURIEREN ──
 *
 * AppDbContext ist unsere Datenbankklasse (in Data/AppDbContext.cs definiert).
 * Sie repräsentiert die Verbindung zur SQLite-Datenbank.
 *
 * AddDbContext<AppDbContext>() registriert den Datenbankkontext im DI-Container.
 * Das bedeutet: überall wo wir AppDbContext brauchen, kann uns das System einen geben.
 *
 * options.UseSqlite() sagt: Benutze SQLite als Datenbanktyp, und hier ist der Pfad.
 * Der "$"-String (Interpolation) setzt den Pfad automatisch ein.
 *
 * Ohne diese Zeilen würde das gesamte Speichern/Lesen von Daten nicht funktionieren.
 */
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

/*
 * ── APP BAUEN ──
 *
 * builder.Build() "baut" die fertige Anwendung aus allen Einstellungen.
 * Ab hier ist die App fertig konfiguriert und wir können sie starten.
 * Man kann builder.Services danach NICHT mehr ändern.
 */
var app = builder.Build();

/*
 * ── DATENBANK INITIALISIEREN ──
 *
 * Dieser Block läuft einmal beim Start und bereitet die Datenbank vor.
 *
 * "using var scope = ..." erstellt einen "Scope" (Gültigkeitsbereich).
 * Das ist nötig, weil AppDbContext nicht einfach so abrufbar ist –
 * wir müssen einen Scope erstellen, um auf den DI-Container zuzugreifen.
 * "using" sorgt dafür, dass der Scope am Ende automatisch aufgeräumt wird.
 *
 * scope.ServiceProvider.GetRequiredService<AppDbContext>():
 *   - ServiceProvider = der DI-Container, der alle Services kennt
 *   - GetRequiredService<AppDbContext>() = "gib mir einen AppDbContext"
 *   - Wenn der nicht verfügbar wäre, würde die App mit einem Fehler abstürzen
 *
 * EnsureCreated() = Erstelle die Datenbanktabellen, falls sie noch nicht existieren.
 *   - Beim ersten Start werden alle Tabellen angelegt
 *   - Bei weiteren Starts passiert nichts (sie existieren ja schon)
 *
 * SeedData.Initialize(context) = Füge Beispieldaten ein (z.B. Standard-Kategorien).
 */
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated(); // Tabellen erstellen falls nicht vorhanden
    SeedData.Initialize(context);     // Beispieldaten einfügen

    /*
     * ALTER TABLE ... ADD COLUMN: Spalte zur bestehenden DB hinzufügen.
     * Das ist eine "Migration" – wenn wir das Modell (Category) um eine neue Spalte
     * erweitern, muss diese auch in der Datenbank angelegt werden.
     *
     * try { } catch { } bedeutet:
     *   - Versuche den SQL-Befehl auszuführen
     *   - Wenn ein Fehler passiert (z.B. Spalte existiert schon), ignoriere ihn einfach
     *   - Ohne try/catch würde die App abstürzen, wenn die Spalte schon existiert
     */
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN Color TEXT NOT NULL DEFAULT '#6c757d'"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Recipes ADD COLUMN Servings INTEGER NOT NULL DEFAULT 4"); } catch { }
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Recipes ADD COLUMN Equipment TEXT"); } catch { }

    // Zwischentabelle für Viele-zu-viele erstellen
    try
    {
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS RecipeCategories (
                RecipeId INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                PRIMARY KEY (RecipeId, CategoryId)
            )");
    } catch { }

    // Bestehende CategoryId-Daten in die neue Zwischentabelle übernehmen
    try
    {
        context.Database.ExecuteSqlRaw(@"
            INSERT OR IGNORE INTO RecipeCategories (RecipeId, CategoryId)
            SELECT Id, CategoryId FROM Recipes
            WHERE CategoryId IS NOT NULL AND CategoryId != 0");
    } catch { }
}

/*
 * ── FEHLERBEHANDLUNG ──
 *
 * Im Entwicklungsmodus zeigt ASP.NET automatisch detaillierte Fehlermeldungen.
 * In der Produktion (auf Railway) wäre das ein Sicherheitsrisiko – dort leiten
 * wir Fehler zur /Error-Seite weiter, die eine benutzerfreundliche Meldung zeigt.
 *
 * IsDevelopment() prüft die Umgebungsvariable ASPNETCORE_ENVIRONMENT.
 * Wenn die "Development" ist, sind wir im Entwicklungsmodus.
 */
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error"); // Im Fehlerfall zur /Error-Seite weiterleiten
}

/*
 * ── HTTPS-UMLEITUNG (AUSKOMMENTIERT) ──
 *
 * Normalerweise würde man HTTP automatisch auf HTTPS umleiten.
 * Auf Railway ist das auskommentiert, weil Railway selbst HTTPS verwaltet
 * (über einen sogenannten Reverse Proxy vor unserer App).
 * Wenn wir es trotzdem aktivieren würden, könnten Weiterleitungsschleifen entstehen.
 */
// Railway übernimmt HTTPS selbst
// app.UseHttpsRedirection();

/*
 * ── MIDDLEWARE KONFIGURIEREN ──
 *
 * Middleware ist wie eine Kette von Stationen, die jede HTTP-Anfrage durchläuft.
 * Reihenfolge ist wichtig! Jede Middleware entscheidet: weiter oder stopp?
 *
 * UseStaticFiles():
 *   - Erlaubt das Ausliefern von statischen Dateien (CSS, JS, Bilder aus wwwroot/)
 *   - Ohne das würde z.B. dein Bootstrap-CSS nicht laden
 *
 * UseRouting():
 *   - Analysiert die URL der Anfrage und entscheidet, welche Seite aufgerufen wird
 *   - Ohne das würde die App nicht wissen, welche Seite bei welcher URL angezeigt werden soll
 *
 * MapRazorPages():
 *   - Registriert alle Razor Pages als Routen
 *   - z.B. /Recipes/Detail → Pages/Recipes/Detail.cshtml
 *   - Ohne das würden alle Seiten 404 (Nicht gefunden) zurückgeben
 */
app.UseStaticFiles(); // Statische Dateien (CSS, JS, Bilder) ausliefern
app.UseRouting();     // URL-Routing aktivieren
app.MapRazorPages();  // Razor Pages als Endpunkte registrieren

/*
 * ── APP STARTEN ──
 *
 * app.Run() startet den Webserver und hält die App am Laufen.
 * Diese Methode blockiert (wartet endlos), bis die App beendet wird.
 * Alles nach app.Run() würde nur ausgeführt, wenn die App stoppt.
 */
app.Run();
