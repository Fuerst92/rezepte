using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using rezepte.Data;
using rezepte.Models;
using rezepte.Services;

namespace rezepte.Pages.Recipes;

public class ImportModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly YouTubeService _youtube;
    private readonly GeminiService _gemini;

    public ImportModel(AppDbContext db, YouTubeService youtube, GeminiService gemini)
    {
        _db = db;
        _youtube = youtube;
        _gemini = gemini;
    }

    // YouTube
    [BindProperty] public string YoutubeUrl { get; set; } = "";

    // Instagram / TikTok
    [BindProperty] public string SocialUrl { get; set; } = "";
    [BindProperty] public string SocialCaption { get; set; } = "";

    [BindProperty] public Recipe Recipe { get; set; } = new();

    public SelectList CategoryOptions { get; set; } = null!;
    public string? ErrorMessage { get; set; }
    public bool RecipeLoaded { get; set; } = false;
    public string? VideoId { get; set; }
    public string ActiveTab { get; set; } = "youtube";

    public void OnGet()
    {
        LoadCategories();
    }

    // ── YouTube analysieren ──
    public async Task<IActionResult> OnPostAnalyzeAsync()
    {
        LoadCategories();
        ActiveTab = "youtube";

        var videoId = _youtube.ExtractVideoId(YoutubeUrl);
        if (videoId == null)
        {
            ErrorMessage = "Kein gültiger YouTube-Link. Bitte prüfe die URL.";
            return Page();
        }

        (string Title, string Description, string ThumbnailUrl)? videoInfo;
        try
        {
            videoInfo = await _youtube.GetVideoInfoAsync(videoId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"YouTube API Fehler: {ex.Message}";
            return Page();
        }

        if (videoInfo == null)
        {
            ErrorMessage = "Video nicht gefunden. Bitte prüfe den Link.";
            return Page();
        }

        RecipeData? recipeData;
        try
        {
            recipeData = await _gemini.ExtractRecipeAsync(videoInfo.Value.Title, videoInfo.Value.Description);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"KI Fehler: {ex.Message}";
            return Page();
        }

        if (recipeData == null)
        {
            ErrorMessage = "KI konnte kein Rezept aus dem Video extrahieren.";
            return Page();
        }

        Recipe.Title = recipeData.Title;
        Recipe.Description = recipeData.Description;
        Recipe.Ingredients = recipeData.Ingredients;
        Recipe.Steps = recipeData.Steps;
        Recipe.ImageUrl = videoInfo.Value.ThumbnailUrl;
        Recipe.VideoUrl = YoutubeUrl;

        VideoId = videoId;
        RecipeLoaded = true;
        return Page();
    }

    // ── Instagram / TikTok analysieren ──
    public async Task<IActionResult> OnPostAnalyzeSocialAsync()
    {
        LoadCategories();
        ActiveTab = "social";

        if (string.IsNullOrWhiteSpace(SocialCaption))
        {
            ErrorMessage = "Bitte füge den Text / die Caption des Posts ein.";
            return Page();
        }

        RecipeData? recipeData;
        try
        {
            // Wir schicken die Caption als "Beschreibung" an die KI
            recipeData = await _gemini.ExtractRecipeAsync("Social Media Post", SocialCaption);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"KI Fehler: {ex.Message}";
            return Page();
        }

        if (recipeData == null)
        {
            ErrorMessage = "KI konnte kein Rezept aus dem Text extrahieren. Ist wirklich ein Rezept im Text?";
            return Page();
        }

        Recipe.Title = recipeData.Title;
        Recipe.Description = recipeData.Description;
        Recipe.Ingredients = recipeData.Ingredients;
        Recipe.Steps = recipeData.Steps;
        Recipe.VideoUrl = string.IsNullOrWhiteSpace(SocialUrl) ? null : SocialUrl;

        RecipeLoaded = true;
        return Page();
    }

    // ── Rezept speichern (für beide Tabs) ──
    public async Task<IActionResult> OnPostSaveAsync()
    {
        ModelState.Remove("Recipe.Category");

        if (!ModelState.IsValid)
        {
            LoadCategories();
            RecipeLoaded = true;
            return Page();
        }

        Recipe.CreatedAt = DateTime.Now;
        _db.Recipes.Add(Recipe);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Recipes/Detail", new { id = Recipe.Id });
    }

    private void LoadCategories()
    {
        CategoryOptions = new SelectList(
            _db.Categories.OrderBy(c => c.Name).ToList(),
            "Id", "Name"
        );
    }
}
