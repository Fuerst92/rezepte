using rezepte.Models;

namespace rezepte.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        if (context.Categories.Any()) return;

        var categories = new List<Category>
        {
            new Category { Name = "Italienisch" },
            new Category { Name = "Asiatisch" },
            new Category { Name = "Desserts" },
            new Category { Name = "Schnell & Einfach" },
            new Category { Name = "Vegetarisch" }
        };

        context.Categories.AddRange(categories);
        context.SaveChanges();

        var pasta = new Recipe
        {
            Title = "Spaghetti Bolognese",
            Description = "Ein Klassiker der italienischen Küche",
            Ingredients = "400g Spaghetti\n300g Hackfleisch\n1 Dose Tomaten\n1 Zwiebel\n2 Knoblauchzehen",
            Steps = "Zwiebel und Knoblauch anbraten\nHackfleisch hinzufügen und braten\nTomaten dazu und 20 Min köcheln\nSpaghetti kochen und servieren",
            CategoryId = categories[0].Id,
            IsFavorite = true
        };

        context.Recipes.Add(pasta);
        context.SaveChanges();
    }
}
