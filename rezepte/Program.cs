using Microsoft.EntityFrameworkCore;
using rezepte.Data;
using rezepte.Services;

var builder = WebApplication.CreateBuilder(args);

// Railway PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRazorPages();

builder.Services.AddHttpClient<YouTubeService>();
builder.Services.AddHttpClient<GeminiService>();

// Datenbankpfad: lokal normal, auf Railway in /data/
string dbPath;
if (Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null)
{
    Directory.CreateDirectory("/data");
    dbPath = "/data/rezepte.db";
}
else
{
    dbPath = "rezepte.db";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    SeedData.Initialize(context);
    // Neue Spalten zur bestehenden DB hinzufügen (falls noch nicht vorhanden)
    try { context.Database.ExecuteSqlRaw("ALTER TABLE Categories ADD COLUMN Color TEXT NOT NULL DEFAULT '#6c757d'"); } catch { }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Railway übernimmt HTTPS selbst
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
