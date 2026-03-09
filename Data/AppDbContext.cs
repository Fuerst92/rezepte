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
        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.Category)
            .WithMany(c => c.Recipes)
            .HasForeignKey(r => r.CategoryId);
    }
}
