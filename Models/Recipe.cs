using System.ComponentModel.DataAnnotations;

namespace rezepte.Models;

public class Recipe
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titel ist erforderlich")]
    [StringLength(100, ErrorMessage = "Maximal 100 Zeichen")]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Beschreibung")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zutaten sind erforderlich")]
    [Display(Name = "Zutaten")]
    public string Ingredients { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zubereitung ist erforderlich")]
    [Display(Name = "Zubereitung")]
    public string Steps { get; set; } = string.Empty;

    [Display(Name = "Bild-URL")]
    [Url(ErrorMessage = "Bitte eine gültige URL eingeben")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Video-Link")]
    [Url(ErrorMessage = "Bitte eine gültige URL eingeben")]
    public string? VideoUrl { get; set; }

    [Display(Name = "Favorit")]
    public bool IsFavorite { get; set; } = false;

    [Display(Name = "Erstellt am")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
