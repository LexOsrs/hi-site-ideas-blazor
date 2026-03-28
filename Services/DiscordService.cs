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
