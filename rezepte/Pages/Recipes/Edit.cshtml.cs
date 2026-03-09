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

        if (!ModelState.IsValid)
        {
            CategoryOptions = new SelectList(
                _db.Categories.OrderBy(c => c.Name).ToList(),
                "Id", "Name"
            );
            return Page();
        }

        _db.Recipes.Update(Recipe);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Recipes/Detail", new { id = Recipe.Id });
    }
}
