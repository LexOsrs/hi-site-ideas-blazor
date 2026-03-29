using Microsoft.EntityFrameworkCore;
using hi_site_ideas_blazor.Components;
using hi_site_ideas_blazor.Data;
using hi_site_ideas_blazor.Models;
using hi_site_ideas_blazor.Services;

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

builder.Services.AddSingleton<DiscordService>();
builder.Services.AddHostedService<GuessSyncService>();

var app = builder.Build();

// Ensure DB exists and seed data
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

    if (!db.Giveaways.Any())
    {
        db.Giveaways.AddRange(GiveawayConstants.GetSeedGiveaways());
        db.SaveChanges();
    }

    if (!db.BingoEvents.Any())
    {
        db.BingoEvents.AddRange(BingoConstants.GetSeedEvents());
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

// Bingo API endpoints
var bingo = app.MapGroup("/api/bingo");

bingo.MapGet("/events", async (IDbContextFactory<AppDbContext> dbFactory, string? status) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var query = db.BingoEvents
        .Include(e => e.Teams)
        .Include(e => e.Tiles)
        .AsQueryable();

    if (Enum.TryParse<BingoEventStatus>(status, true, out var s))
        query = query.Where(e => e.Status == s);

    return await query.OrderByDescending(e => e.CreatedAt)
        .Select(e => new
        {
            e.Id, e.Title, e.Description, e.BoardSize, Status = e.Status.ToString(),
            e.StartsAt, e.EndsAt, e.CreatedAt,
            TeamCount = e.Teams.Count, TileCount = e.Tiles.Count,
        }).ToListAsync();
});

bingo.MapGet("/events/{id:int}", async (int id, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var ev = await db.BingoEvents
        .Include(e => e.Tiles).ThenInclude(t => t.TeamTiles)
        .Include(e => e.Teams).ThenInclude(t => t.TeamTiles)
        .FirstOrDefaultAsync(e => e.Id == id);
    if (ev == null) return Results.NotFound();

    return Results.Ok(new
    {
        ev.Id, ev.Title, ev.Description, ev.BoardSize, Status = ev.Status.ToString(),
        ev.StartsAt, ev.EndsAt, ev.CreatedAt,
        Tiles = ev.Tiles.OrderBy(t => t.Position).Select(t => new
        {
            t.Id, t.Position, t.Title, t.Description, t.ImageUrl, t.Points,
            TeamStatuses = t.TeamTiles.Select(tt => new
            {
                TeamId = tt.BingoTeamId, Status = tt.Status.ToString(), tt.CompletedAt,
            }),
        }),
        Teams = ev.Teams.Select(t => new
        {
            t.Id, t.Name, t.Color,
            CompletedTiles = t.TeamTiles.Count(tt => tt.Status == TileStatus.Completed),
            Points = t.TeamTiles.Where(tt => tt.Status == TileStatus.Completed)
                .Sum(tt => ev.Tiles.FirstOrDefault(tile => tile.Id == tt.BingoTileId)?.Points ?? 1),
        }),
    });
});

bingo.MapGet("/events/{id:int}/teams/{teamId:int}", async (int id, int teamId, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var ev = await db.BingoEvents
        .Include(e => e.Tiles).ThenInclude(t => t.TeamTiles.Where(tt => tt.BingoTeamId == teamId))
        .Include(e => e.Teams)
        .FirstOrDefaultAsync(e => e.Id == id);
    if (ev == null) return Results.NotFound();

    var team = ev.Teams.FirstOrDefault(t => t.Id == teamId);
    if (team == null) return Results.NotFound();

    return Results.Ok(new
    {
        Event = new { ev.Id, ev.Title, ev.BoardSize, Status = ev.Status.ToString() },
        Team = new { team.Id, team.Name, team.Color },
        Tiles = ev.Tiles.OrderBy(t => t.Position).Select(t =>
        {
            var tt = t.TeamTiles.FirstOrDefault();
            return new
            {
                t.Id, t.Position, t.Title, t.Description, t.ImageUrl, t.Points,
                Status = (tt?.Status ?? TileStatus.NotStarted).ToString(),
                tt?.CompletedAt,
            };
        }),
    });
});

bingo.MapPost("/events/{id:int}/teams/{teamId:int}/tiles/{tileId:int}", async (int id, int teamId, int tileId, TileUpdateRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    if (!Enum.TryParse<TileStatus>(req.Status, true, out var status))
        return Results.BadRequest("Invalid status. Use: NotStarted, InProgress, Completed");

    var tile = await db.BingoTiles.FirstOrDefaultAsync(t => t.Id == tileId && t.BingoEventId == id);
    if (tile == null) return Results.NotFound("Tile not found");

    var team = await db.BingoTeams.FirstOrDefaultAsync(t => t.Id == teamId && t.BingoEventId == id);
    if (team == null) return Results.NotFound("Team not found");

    var existing = await db.BingoTeamTiles.FirstOrDefaultAsync(tt => tt.BingoTeamId == teamId && tt.BingoTileId == tileId);
    if (existing != null)
    {
        existing.Status = status;
        existing.CompletedAt = status == TileStatus.Completed ? DateTime.UtcNow : null;
        existing.Proof = req.Proof ?? existing.Proof;
    }
    else
    {
        db.BingoTeamTiles.Add(new BingoTeamTile
        {
            BingoTeamId = teamId,
            BingoTileId = tileId,
            Status = status,
            CompletedAt = status == TileStatus.Completed ? DateTime.UtcNow : null,
            Proof = req.Proof,
        });
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { Status = status.ToString() });
});

bingo.MapPost("/events", async (BingoEventCreateRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var ev = new BingoEvent
    {
        Title = req.Title,
        Description = req.Description,
        BoardSize = Math.Clamp(req.BoardSize, 3, 5),
        Status = Enum.TryParse<BingoEventStatus>(req.Status, true, out var s) ? s : BingoEventStatus.Upcoming,
        StartsAt = req.StartsAt,
        EndsAt = req.EndsAt,
    };
    db.BingoEvents.Add(ev);
    await db.SaveChangesAsync();
    return Results.Created($"/api/bingo/events/{ev.Id}", new { ev.Id, ev.Title });
});

bingo.MapPut("/events/{id:int}/status", async (int id, StatusUpdateRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var ev = await db.BingoEvents.FindAsync(id);
    if (ev == null) return Results.NotFound();

    if (!Enum.TryParse<BingoEventStatus>(req.Status, true, out var status))
        return Results.BadRequest("Invalid status. Use: Upcoming, Active, Completed");

    ev.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok(new { ev.Id, Status = status.ToString() });
});

bingo.MapGet("/events/{id:int}/export", async (int id, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var ev = await db.BingoEvents
        .Include(e => e.Tiles).ThenInclude(t => t.TeamTiles)
        .Include(e => e.Teams)
        .FirstOrDefaultAsync(e => e.Id == id);
    if (ev == null) return Results.NotFound();

    var csv = new System.Text.StringBuilder();
    csv.Append("Position,Title,Description,Points");
    foreach (var team in ev.Teams)
        csv.Append($",{Escape(team.Name)}");
    csv.AppendLine();

    foreach (var tile in ev.Tiles.OrderBy(t => t.Position))
    {
        csv.Append($"{tile.Position + 1},{Escape(tile.Title)},{Escape(tile.Description ?? "")},{tile.Points}");
        foreach (var team in ev.Teams)
        {
            var tt = tile.TeamTiles.FirstOrDefault(x => x.BingoTeamId == team.Id);
            csv.Append($",{(tt?.Status ?? TileStatus.NotStarted)}");
        }
        csv.AppendLine();
    }

    var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    return Results.File(bytes, "text/csv", $"bingo-{ev.Id}-{ev.Title.Replace(' ', '-').ToLower()}.csv");

    static string Escape(string s) => s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
});

// Submission endpoints
bingo.MapPost("/submissions/{teamTileId:int}", async (int teamTileId, SubmissionRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var teamTile = await db.BingoTeamTiles.FindAsync(teamTileId);
    if (teamTile == null) return Results.NotFound("Team tile not found");

    var sub = new BingoSubmission
    {
        BingoTeamTileId = teamTileId,
        SubmittedBy = req.SubmittedBy,
        ImageUrl = req.ImageUrl,
        Caption = req.Caption,
        Type = Enum.TryParse<SubmissionType>(req.Type, true, out var t) ? t : SubmissionType.Progress,
        Status = SubmissionStatus.Pending,
        Entries = req.Entries?.Select(e => new BingoSubmissionEntry { RequirementLabel = e.Label, Amount = e.Amount }).ToList() ?? [],
    };
    db.BingoSubmissions.Add(sub);
    await db.SaveChangesAsync();
    return Results.Ok(new { sub.Id, Status = sub.Status.ToString() });
});

bingo.MapPut("/submissions/{submissionId:int}/review", async (int submissionId, ReviewRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var sub = await db.BingoSubmissions.FindAsync(submissionId);
    if (sub == null) return Results.NotFound();

    if (!Enum.TryParse<SubmissionStatus>(req.Status, true, out var status) || status == SubmissionStatus.Pending)
        return Results.BadRequest("Status must be Approved or Denied");

    sub.Status = status;
    sub.ReviewedBy = req.ReviewedBy ?? "Admin";
    sub.ReviewedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { sub.Id, Status = status.ToString() });
});

// Discord integration endpoints
bingo.MapPost("/discord/submit", async (DiscordSubmitRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();

    // Find team tile by Discord thread ID
    var teamTile = await db.BingoTeamTiles
        .Include(tt => tt.Tile)
        .Include(tt => tt.Team).ThenInclude(t => t.Members)
        .FirstOrDefaultAsync(tt => tt.DiscordThreadId == req.ThreadId);

    if (teamTile == null) return Results.NotFound("No tile found for this thread");

    // Verify player is on this team
    var playerName = req.PlayerName;
    var team = teamTile.Team;

    var sub = new BingoSubmission
    {
        BingoTeamTileId = teamTile.Id,
        SubmittedBy = playerName,
        ImageUrl = req.ImageUrl,
        Caption = req.Caption,
        DiscordMessageId = req.MessageId,
        Type = req.IsStart ? SubmissionType.Start : SubmissionType.Progress,
        Status = SubmissionStatus.Pending,
    };
    db.BingoSubmissions.Add(sub);
    await db.SaveChangesAsync();

    return Results.Ok(new { sub.Id, TileTitle = teamTile.Tile.Title, TeamName = team.Name, sub.Status });
});

bingo.MapPut("/discord/teams/{teamId:int}/channel", async (int teamId, SetChannelRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var team = await db.BingoTeams.FindAsync(teamId);
    if (team == null) return Results.NotFound();
    team.DiscordChannelId = req.ChannelId;
    await db.SaveChangesAsync();
    return Results.Ok(new { team.Id, team.Name, req.ChannelId });
});

bingo.MapPut("/discord/teamtiles/{teamTileId:int}/thread", async (int teamTileId, SetThreadRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var tt = await db.BingoTeamTiles.FindAsync(teamTileId);
    if (tt == null) return Results.NotFound();
    tt.DiscordThreadId = req.ThreadId;
    await db.SaveChangesAsync();
    return Results.Ok(new { tt.Id, req.ThreadId });
});

bingo.MapGet("/discord/event/{eventId:int}/teams", async (int eventId, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var teams = await db.BingoTeams
        .Where(t => t.BingoEventId == eventId)
        .Select(t => new { t.Id, t.Name, t.DiscordChannelId })
        .ToListAsync();
    return Results.Ok(teams);
});

bingo.MapGet("/discord/event/{eventId:int}/tiles", async (int eventId, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var tiles = await db.BingoTiles
        .Where(t => t.BingoEventId == eventId)
        .OrderBy(t => t.Position)
        .Select(t => new { t.Id, t.Position, t.Title, t.Points })
        .ToListAsync();
    return Results.Ok(tiles);
});

bingo.MapGet("/discord/event/{eventId:int}/teamtiles", async (int eventId, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var teamTiles = await db.BingoTeamTiles
        .Include(tt => tt.Tile)
        .Include(tt => tt.Team)
        .Where(tt => tt.Tile.BingoEventId == eventId)
        .Select(tt => new { tt.Id, TeamId = tt.BingoTeamId, TeamName = tt.Team.Name, TileId = tt.BingoTileId, TileTitle = tt.Tile.Title, tt.DiscordThreadId })
        .ToListAsync();
    return Results.Ok(teamTiles);
});

bingo.MapGet("/discord/submission/by-message/{messageId}", async (ulong messageId, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var sub = await db.BingoSubmissions
        .Include(s => s.Entries)
        .FirstOrDefaultAsync(s => s.DiscordMessageId == messageId);
    if (sub == null) return Results.NotFound();

    var teamTile = await db.BingoTeamTiles
        .Include(tt => tt.Tile).ThenInclude(t => t.RequirementGroups).ThenInclude(g => g.Options).ThenInclude(o => o.Requirements)
        .Include(tt => tt.Team)
        .FirstOrDefaultAsync(tt => tt.Id == sub.BingoTeamTileId);

    var requirementLabels = teamTile?.Tile.RequirementGroups
        .SelectMany(g => g.Options).SelectMany(o => o.Requirements)
        .Select(r => r.Label).Distinct().ToList() ?? [];

    return Results.Ok(new
    {
        sub.Id, sub.SubmittedBy, sub.ImageUrl, sub.Caption, sub.DiscordMessageId,
        Status = sub.Status.ToString(), Type = sub.Type.ToString(),
        TileTitle = teamTile?.Tile.Title,
        TeamName = teamTile?.Team.Name,
        Entries = sub.Entries.Select(e => new { e.RequirementLabel, e.Amount }),
        RequirementLabels = requirementLabels,
    });
});

bingo.MapGet("/discord/submission/{submissionId:int}", async (int submissionId, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var sub = await db.BingoSubmissions
        .Include(s => s.Entries)
        .FirstOrDefaultAsync(s => s.Id == submissionId);
    if (sub == null) return Results.NotFound();

    var teamTile = await db.BingoTeamTiles
        .Include(tt => tt.Tile).ThenInclude(t => t.RequirementGroups).ThenInclude(g => g.Options).ThenInclude(o => o.Requirements)
        .FirstOrDefaultAsync(tt => tt.Id == sub.BingoTeamTileId);

    var requirementLabels = teamTile?.Tile.RequirementGroups
        .SelectMany(g => g.Options).SelectMany(o => o.Requirements)
        .Select(r => r.Label).Distinct().ToList() ?? [];

    return Results.Ok(new
    {
        sub.Id, sub.SubmittedBy, sub.ImageUrl, sub.Caption, sub.DiscordMessageId,
        Status = sub.Status.ToString(), Type = sub.Type.ToString(),
        TileTitle = teamTile?.Tile.Title,
        Entries = sub.Entries.Select(e => new { e.RequirementLabel, e.Amount }),
        RequirementLabels = requirementLabels,
    });
});

bingo.MapPut("/discord/submission/{submissionId:int}/review", async (int submissionId, DiscordReviewRequest req, IDbContextFactory<AppDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var sub = await db.BingoSubmissions.Include(s => s.Entries).FirstOrDefaultAsync(s => s.Id == submissionId);
    if (sub == null) return Results.NotFound();

    if (!Enum.TryParse<SubmissionStatus>(req.Status, true, out var status) || status == SubmissionStatus.Pending)
        return Results.BadRequest("Status must be Approved or Denied");

    // Progress submissions need at least one entry to be approved
    if (status == SubmissionStatus.Approved && sub.Type == SubmissionType.Progress)
    {
        var entries = req.Entries ?? [];
        var hasEntries = sub.Entries.Any(e => !string.IsNullOrEmpty(e.RequirementLabel) && e.Amount > 0)
            || entries.Any(e => !string.IsNullOrEmpty(e.Label) && e.Amount > 0);
        if (!hasEntries)
            return Results.BadRequest("Progress submissions need at least one item assigned before approval");
    }

    sub.Status = status;
    sub.ReviewedBy = req.ReviewedBy ?? "Moderator";
    sub.ReviewedAt = DateTime.UtcNow;

    if (req.Entries != null)
    {
        db.BingoSubmissionEntries.RemoveRange(sub.Entries);
        sub.Entries = req.Entries.Select(e => new BingoSubmissionEntry { RequirementLabel = e.Label, Amount = e.Amount }).ToList();
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { sub.Id, Status = status.ToString() });
});

await app.Services.GetRequiredService<DiscordService>().SendStartupMessage();

app.Run();

// API request types
record TileUpdateRequest(string Status, string? Proof);
record BingoEventCreateRequest(string Title, string? Description, int BoardSize, string? Status, DateTime? StartsAt, DateTime? EndsAt);
record StatusUpdateRequest(string Status);
record SubmissionRequest(string SubmittedBy, string ImageUrl, string? Caption, string Type, SubmissionEntryRequest[]? Entries);
record SubmissionEntryRequest(string Label, int Amount);
record ReviewRequest(string Status, string? ReviewedBy);
record DiscordSubmitRequest(ulong ThreadId, ulong MessageId, string PlayerName, string ImageUrl, string? Caption, bool IsStart);
record SetChannelRequest(ulong ChannelId);
record SetThreadRequest(ulong ThreadId);
record DiscordReviewRequest(string Status, string? ReviewedBy, SubmissionEntryRequest[]? Entries);
