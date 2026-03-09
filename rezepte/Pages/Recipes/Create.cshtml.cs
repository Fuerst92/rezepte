using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public SelectList CategoryOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        CategoryOptions = new SelectList(
            await Task.Run(() => _db.Categories.OrderBy(c => c.Name).ToList()),
            "Id", "Name"
        );
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModelState.Remove("Recipe.Category");

        if (!ModelState.IsValid)
        {
            CategoryOptions = new SelectList(
                _db.Categories.OrderBy(c => c.Name).ToList(),
                "Id", "Name"
            );
            return Page();
        }

        Recipe.CreatedAt = DateTime.Now;
        _db.Recipes.Add(Recipe);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Recipes/Detail", new { id = Recipe.Id });
    }
}
