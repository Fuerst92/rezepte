using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using rezepte.Data;
using rezepte.Models;

namespace rezepte.Pages.Recipes;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Recipe Recipe { get; set; } = new();

    // Alle Kategorien für die Checkboxen
    public List<Category> AllCategories { get; set; } = new();

    // Die vom Benutzer ausgewählten Kategorie-IDs (aus den Checkboxen)
    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = new();

    public async Task OnGetAsync()
    {
        AllCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Recipe.Categories");

        if (!ModelState.IsValid)
        {
            AllCategories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return Page();
        }

        // Ausgewählte Kategorien aus der Datenbank laden und dem Rezept zuweisen
        var selectedCategories = await _db.Categories
            .Where(c => SelectedCategoryIds.Contains(c.Id))
            .ToListAsync();

        Recipe.CreatedAt = DateTime.Now;
        Recipe.Categories = selectedCategories;
        _db.Recipes.Add(Recipe);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Recipes/Detail", new { id = Recipe.Id });
    }
}
