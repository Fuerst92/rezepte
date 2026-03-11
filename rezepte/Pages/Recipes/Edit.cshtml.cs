using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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

    public List<Category> AllCategories { get; set; } = new();

    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var recipe = await _db.Recipes
            .Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recipe == null) return NotFound();

        Recipe = recipe;
        AllCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        // Bestehende Kategorien vorauswählen
        SelectedCategoryIds = recipe.Categories.Select(c => c.Id).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Recipe.Categories");

        if (!ModelState.IsValid)
        {
            AllCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return Page();
        }

        // Rezept mit aktuellen Kategorien aus der DB laden
        var recipe = await _db.Recipes
            .Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == Recipe.Id);

        if (recipe == null) return NotFound();

        // Felder übernehmen
        recipe.Title = Recipe.Title;
        recipe.Description = Recipe.Description;
        recipe.Ingredients = Recipe.Ingredients;
        recipe.Steps = Recipe.Steps;
        recipe.ImageUrl = Recipe.ImageUrl;
        recipe.VideoUrl = Recipe.VideoUrl;
        recipe.IsFavorite = Recipe.IsFavorite;
        recipe.Servings = Recipe.Servings;
        recipe.Equipment = Recipe.Equipment;

        // Kategorien aktualisieren
        var selectedCategories = await _db.Categories
            .Where(c => SelectedCategoryIds.Contains(c.Id))
            .ToListAsync();

        recipe.Categories.Clear();
        foreach (var cat in selectedCategories)
            recipe.Categories.Add(cat);

        await _db.SaveChangesAsync();

        return RedirectToPage("/Recipes/Detail", new { id = recipe.Id });
    }
}
