using NetCord;
using NetCord.Rest;
using hi_site_ideas_blazor.Models;

namespace hi_site_ideas_blazor.Services;

public class DiscordService(IConfiguration config, ILogger<DiscordService> logger)
{
    public RestClient? CreateClient()
    {
        var token = config["Discord:BotToken"];
        return string.IsNullOrEmpty(token) ? null : new RestClient(new BotToken(token));
    }

    public ulong? GetChannelId()
    {
        var str = config["Discord:ChannelId"];
        return ulong.TryParse(str, out var id) ? id : null;
    }

    public async Task SendStartupMessage()
    {
        using var client = CreateClient();
        if (client == null) return;

        try
        {
            await client.SendMessageAsync(1060310256833527931, new MessageProperties()
                .WithContent($"Hi-site started up ok — {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send startup message to Discord");
        }
    }

    public async Task CreateGiveawayPost(Giveaway giveaway)
    {
        using var client = CreateClient();
        var channelId = GetChannelId();
        if (client == null || channelId == null) return;

        try
        {
            var hasBoss = !string.IsNullOrEmpty(giveaway.Boss);
            var content = BuildMessage(giveaway, hasBoss);

            var message = await client.SendMessageAsync(channelId.Value, new MessageProperties().WithContent(content));
            giveaway.DiscordMessageId = message.Id;

            if (hasBoss)
            {
                var thread = await client.CreateGuildThreadAsync(channelId.Value, message.Id,
                    new GuildThreadFromMessageProperties($"{giveaway.Title} — Guesses"));
                giveaway.DiscordThreadId = thread.Id;
            }
            else
            {
                await client.AddMessageReactionAsync(channelId.Value, message.Id, new ReactionEmojiProperties("🎉"));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create Discord giveaway post for {Title}", giveaway.Title);
        }
    }

    public ulong? GetGuildId()
    {
        var str = config["Discord:GuildId"];
        return ulong.TryParse(str, out var id) ? id : null;
    }

    /// <summary>Create a Discord text channel for a bingo team. Returns the channel ID.</summary>
    public async Task<ulong?> CreateBingoTeamChannel(BingoTeam team)
    {
        using var client = CreateClient();
        var guildId = GetGuildId();
        if (client == null || !guildId.HasValue) return null;

        try
        {
            var channelName = team.Name.ToLower()
                .Replace("'", "").Replace("'", "")
                .Replace(" ", "-").Replace("--", "-");

            var channel = await client.CreateGuildChannelAsync(guildId.Value,
                new GuildChannelProperties(channelName, ChannelType.TextGuildChannel));

            return channel.Id;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create Discord channel for team {Team}", team.Name);
            return null;
        }
    }

    /// <summary>Create tile threads in a team's Discord channel. Returns the number of threads created.</summary>
    public async Task<int> CreateBingoThreads(BingoTeam team, IEnumerable<BingoTile> tiles, Dictionary<int, BingoTeamTile> teamTileMap)
    {
        using var client = CreateClient();
        if (client == null || !team.DiscordChannelId.HasValue) return 0;

        var channelId = team.DiscordChannelId.Value;
        int created = 0;

        foreach (var tile in tiles.OrderBy(t => t.Position))
        {
            // Skip if this team tile already has a thread
            if (teamTileMap.TryGetValue(tile.Id, out var tt) && tt.DiscordThreadId.HasValue)
                continue;

            try
            {
                // Post a message first, then create a thread from it
                var msg = await client.SendMessageAsync(channelId, new MessageProperties()
                    .WithContent($"**#{tile.Position + 1} — {tile.Title}** ({tile.Points} pts)\nPost screenshots here for moderator approval."));

                var thread = await client.CreateGuildThreadAsync(channelId, msg.Id,
                    new GuildThreadFromMessageProperties(tile.Title));

                // Update the team tile with the thread ID
                if (tt != null)
                    tt.DiscordThreadId = thread.Id;

                created++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create thread for tile {Title} in channel {ChannelId}", tile.Title, channelId);
            }
        }

        return created;
    }

    /// <summary>Add a reaction to a Discord message (e.g. ✅ when approved from site).</summary>
    public async Task AddReaction(ulong channelId, ulong messageId, string emoji)
    {
        using var client = CreateClient();
        if (client == null) return;

        try
        {
            await client.AddMessageReactionAsync(channelId, messageId, new ReactionEmojiProperties(emoji));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to add reaction to message {MessageId}", messageId);
        }
    }

    /// <summary>Post a tile completion message in the tile's thread.</summary>
    public async Task NotifyTileComplete(BingoTeamTile teamTile, BingoTile tile)
    {
        using var client = CreateClient();
        if (client == null || !teamTile.DiscordThreadId.HasValue) return;

        try
        {
            await client.SendMessageAsync(teamTile.DiscordThreadId.Value, new MessageProperties()
                .WithContent($"🎉 **{tile.Title}** is complete! (+{tile.Points} pts)"));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send tile complete notification");
        }
    }

    /// <summary>Post a tile completion announcement in the team's channel.</summary>
    public async Task NotifyTeamChannel(BingoTeam team, string message)
    {
        using var client = CreateClient();
        if (client == null || !team.DiscordChannelId.HasValue) return;

        try
        {
            await client.SendMessageAsync(team.DiscordChannelId.Value, new MessageProperties()
                .WithContent(message));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send team channel notification");
        }
    }

    private static string BuildMessage(Giveaway giveaway, bool hasBoss)
    {
        var prizeLines = string.Join("\n", giveaway.Prizes.Select(p =>
            $"**{p.Label}:** {p.Reward}{(p.Count.HasValue ? $" (x{p.Count})" : "")}"));

        var lines = new List<string> { $"🎁 **{giveaway.Title}**\n" };
        if (!string.IsNullOrEmpty(giveaway.Player))
            lines.Add($"**Player:** {giveaway.Player}");
        if (!string.IsNullOrEmpty(giveaway.Boss))
            lines.Add($"**Boss:** {giveaway.Boss}");
        if (!string.IsNullOrEmpty(giveaway.Item))
            lines.Add($"**Item:** {giveaway.Item}");
        if (giveaway.StartKc > 0)
            lines.Add($"**Start KC:** {giveaway.StartKc:N0}");
        if (giveaway.GuessingClosesAt.HasValue)
            lines.Add($"**Guessing closes:** {giveaway.GuessingClosesAt.Value:MMM d, h:mm tt}");
        lines.Add("");
        lines.Add(prizeLines);

        if (hasBoss)
            lines.Add("\n📝 Post your KC guess in the thread below!");
        else
            lines.Add("\n🎉 React to this message to enter!");

        return string.Join("\n", lines);
    }
}
