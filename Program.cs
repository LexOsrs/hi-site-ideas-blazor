using Microsoft.EntityFrameworkCore;
using hi_site_ideas_blazor.Components;
using hi_site_ideas_blazor.Data;
using hi_site_ideas_blazor.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var sqlServerConn = builder.Configuration.GetConnectionString("SqlServer");
if (!string.IsNullOrEmpty(sqlServerConn))
{
    builder.Services.AddDbContextFactory<AppDbContext>(options =>
        options.UseSqlServer(sqlServerConn));
}
else
{
    builder.Services.AddDbContextFactory<AppDbContext>(options =>
        options.UseSqlite("Data Source=hisite.db"));
}

var app = builder.Build();

// Ensure DB exists and seed members
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Members.Any())
    {
        db.Members.AddRange(
            GiveawayConstants.Members.Select(name => new Member { DisplayName = name })
        );
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
