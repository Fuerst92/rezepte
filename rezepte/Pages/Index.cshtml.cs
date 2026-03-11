using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using rezepte.Data;
using rezepte.Models;

namespace rezepte.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Recipe> Recipes { get; set; } = new();
    public List<Category> Categories { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? SelectedCategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool FavoritesOnly { get; set; }

    // SupportsGet = true bedeutet: der Wert kommt aus der URL (?searchQuery=...)
    // So bleibt die Suche auch nach dem Neuladen erhalten
    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();

        // AsQueryable() = Abfrage wird noch nicht ausgeführt, wir bauen sie erstmal zusammen
        var query = _db.Recipes.Include(r => r.Categories).AsQueryable();

        // Filter: Kategorie (prüft ob Rezept diese Kategorie enthält)
        if (SelectedCategoryId.HasValue)
            query = query.Where(r => r.Categories.Any(c => c.Id == SelectedCategoryId.Value));

        // Filter: Nur Favoriten
        if (FavoritesOnly)
            query = query.Where(r => r.IsFavorite);

        // Filter: Suchbegriff in Titel ODER Zutaten
        // string.IsNullOrWhiteSpace prüft ob die Suche leer ist
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var term = SearchQuery.ToLower(); // Klein schreiben für Groß-/Kleinschreibung egal
            query = query.Where(r =>
                r.Title.ToLower().Contains(term) ||       // Titel enthält Suchbegriff
                r.Ingredients.ToLower().Contains(term));  // ODER Zutaten enthalten ihn
        }

        Recipes = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleFavoriteAsync(int id)
    {
        var recipe = await _db.Recipes.FindAsync(id);
        if (recipe == null) return NotFound();

        recipe.IsFavorite = !recipe.IsFavorite;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { SelectedCategoryId, FavoritesOnly });
    }
}
