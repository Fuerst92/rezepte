using System.ComponentModel.DataAnnotations;

namespace rezepte.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Kategorie")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string Color { get; set; } = "#6c757d";

    public List<Recipe> Recipes { get; set; } = new();
}
