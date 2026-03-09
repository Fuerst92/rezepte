using Microsoft.EntityFrameworkCore;
using rezepte.Data;

var builder = WebApplication.CreateBuilder(args);

// Railway gibt den PORT als Umgebungsvariable vor
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRazorPages();

// Datenbankpfad: lokal normal, auf Railway in /data/ (persistenter Speicher)
var dbPath = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null
    ? "/data/rezepte.db"
    : "rezepte.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    SeedData.Initialize(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // HTTPS-Redirect deaktivieren auf Railway (Railway macht das selbst)
    // app.UseHsts();
}

// app.UseHttpsRedirection(); // Railway übernimmt HTTPS
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
