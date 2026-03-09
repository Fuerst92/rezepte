using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using rezepte.Data;
using rezepte.Models;

namespace rezepte.Pages.Recipes;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailModel(AppDbContext db)
    {
        _db = db;
    }

    public Recipe Recipe { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null) return NotFound();

        Recipe = recipe;
        return Page();
    }

    public async Task<IActionResult> OnPostToggleFavoriteAsync(int id)
    {
        var recipe = await _db.Recipes.FindAsync(id);
        if (recipe == null) return NotFound();

        recipe.IsFavorite = !recipe.IsFavorite;
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var recipe = await _db.Recipes.FindAsync(id);
        if (recipe != null)
        {
            _db.Recipes.Remove(recipe);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage("/Index");
    }
}
