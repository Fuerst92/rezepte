using Microsoft.EntityFrameworkCore;
using rezepte.Models;

namespace rezepte.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Viele-zu-viele: Recipe <-> Category
        // EF erstellt automatisch eine Zwischentabelle "RecipeCategories"
        // mit den Spalten RecipeId und CategoryId
        modelBuilder.Entity<Recipe>()
            .HasMany(r => r.Categories)
            .WithMany(c => c.Recipes)
            .UsingEntity<Dictionary<string, object>>(
                "RecipeCategories",
                j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                j => j.HasOne<Recipe>().WithMany().HasForeignKey("RecipeId")
            );
    }
}
