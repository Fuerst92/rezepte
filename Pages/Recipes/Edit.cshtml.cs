using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using rezepte.Data;
using rezepte.Models;

namespace rezepte.Pages.Recipes;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Recipe Recipe { get; set; } = null!;

    public SelectList CategoryOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var recipe = await _db.Recipes.FindAsync(id);
        if (recipe == null) return NotFound();

        Recipe = recipe;
        CategoryOptions = new SelectList(
            _db.Categories.OrderBy(c => c.Name).ToList(),
            "Id", "Name"
        );
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Recipe.Category");

        // Leere Strings zu null konvertieren, sonst schlägt [Url]-Validierung fehl
        if (string.IsNullOrWhiteSpace(Recipe.ImageUrl)) Recipe.ImageUrl = null;
        if (string.IsNullOrWhiteSpace(Recipe.VideoUrl)) Recipe.VideoUrl = null;

        ModelState.Clear();
        TryValidateModel(Recipe);

        if (!ModelState.IsValid)
        {
            CategoryOptions = new SelectList(
                _db.Categories.OrderBy(c => c.Name).ToList(),
                "Id", "Name"
            );
            return Page();
        }

        var existing = await _db.Recipes.FindAsync(Recipe.Id);
        if (existing == null) return NotFound();

        existing.Title = Recipe.Title;
        existing.Description = Recipe.Description;
        existing.Ingredients = Recipe.Ingredients;
        existing.Steps = Recipe.Steps;
        existing.ImageUrl = Recipe.ImageUrl;
        existing.VideoUrl = Recipe.VideoUrl;
        existing.CategoryId = Recipe.CategoryId;
        existing.IsFavorite = Recipe.IsFavorite;

        await _db.SaveChangesAsync();

        return RedirectToPage("/Recipes/Detail", new { id = Recipe.Id });
    }
}
