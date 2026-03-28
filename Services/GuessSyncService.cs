using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using hi_site_ideas_blazor.Data;
using hi_site_ideas_blazor.Models;

namespace hi_site_ideas_blazor.Services;

public class GuessSyncService(
    IDbContextFactory<AppDbContext> dbFactory,
    DiscordService discord,
    IConfiguration config,
    ILogger<GuessSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before first poll
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncGuesses(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Guess sync failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);
        }
    }

    private async Task SyncGuesses(CancellationToken ct)
    {
        using var client = discord.CreateClient();
        if (client == null) return;

        var guildIdStr = config["Discord:GuildId"];
        if (!ulong.TryParse(guildIdStr, out var guildId)) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var activeWithThreads = await db.Giveaways
            .Include(g => g.Guesses)
            .Where(g => g.Status == GiveawayStatus.Active && g.DiscordThreadId != null)
            .ToListAsync(ct);

        foreach (var giveaway in activeWithThreads)
        {
            try
            {
                await SyncThreadGuesses(client, db, giveaway, guildId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to sync guesses for giveaway {Id}", giveaway.Id);
            }
        }
    }

    private async Task SyncThreadGuesses(RestClient client, AppDbContext db, Giveaway giveaway, ulong guildId, CancellationToken ct)
    {
        var threadId = giveaway.DiscordThreadId!.Value;
        var existingNames = giveaway.Guesses.Select(g => g.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Fetch messages from the thread (up to 100)
        var messages = await client.GetMessagesAsync(threadId).Take(100).ToListAsync(ct);

        var newGuesses = new List<Guess>();
        foreach (var msg in messages)
        {
            if (msg.Author.IsBot) continue;

            var text = msg.Content.Trim().Replace(",", "").Replace(" ", "");
            if (!int.TryParse(text, out var kc) || kc <= 0) continue;

            // Get server nickname, fall back to global name, then username
            string name;
            try
            {
                var member = await client.GetGuildUserAsync(guildId, msg.Author.Id);
                name = member.Nickname ?? msg.Author.GlobalName ?? msg.Author.Username;
            }
            catch
            {
                name = msg.Author.GlobalName ?? msg.Author.Username;
            }

            if (existingNames.Contains(name)) continue;

            newGuesses.Add(new Guess
            {
                GiveawayId = giveaway.Id,
                Name = name,
                GuessKc = kc,
            });
            existingNames.Add(name);
        }

        if (newGuesses.Count > 0)
        {
            db.Guesses.AddRange(newGuesses);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Synced {Count} new guesses for giveaway {Id} ({Title})",
                newGuesses.Count, giveaway.Id, giveaway.Title);
        }
    }
}
