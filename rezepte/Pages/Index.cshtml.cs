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

    public async Task OnGetAsync()
    {
        Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();

        var query = _db.Recipes.Include(r => r.Category).AsQueryable();

        if (SelectedCategoryId.HasValue)
            query = query.Where(r => r.CategoryId == SelectedCategoryId.Value);

        if (FavoritesOnly)
            query = query.Where(r => r.IsFavorite);

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
