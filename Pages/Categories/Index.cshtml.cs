using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using rezepte.Data;
using rezepte.Models;

namespace rezepte.Pages.Categories;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Category> Categories { get; set; } = new();

    [BindProperty]
    public string NewCategoryName { get; set; } = "";

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
        LoadCategories();
    }

    // Neue Kategorie hinzufügen
    public IActionResult OnPostAdd()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            ErrorMessage = "Bitte einen Namen eingeben.";
            LoadCategories();
            return Page();
        }

        if (_db.Categories.Any(c => c.Name == NewCategoryName.Trim()))
        {
            ErrorMessage = $"Die Kategorie \"{NewCategoryName}\" existiert bereits.";
            LoadCategories();
            return Page();
        }

        _db.Categories.Add(new Category { Name = NewCategoryName.Trim() });
        _db.SaveChanges();
        SuccessMessage = $"Kategorie \"{NewCategoryName.Trim()}\" wurde erstellt!";
        LoadCategories();
        return Page();
    }

    // Kategorie löschen
    public IActionResult OnPostDelete(int id)
    {
        var category = _db.Categories.Find(id);
        if (category == null) return NotFound();

        // Prüfen ob noch Rezepte in dieser Kategorie sind
        if (_db.Recipes.Any(r => r.CategoryId == id))
        {
            ErrorMessage = $"Kategorie \"{category.Name}\" kann nicht gelöscht werden – es gibt noch Rezepte darin.";
            LoadCategories();
            return Page();
        }

        _db.Categories.Remove(category);
        _db.SaveChanges();
        SuccessMessage = $"Kategorie \"{category.Name}\" wurde gelöscht.";
        LoadCategories();
        return Page();
    }

    private void LoadCategories()
    {
        Categories = _db.Categories
            .OrderBy(c => c.Name)
            .ToList();
    }
}
