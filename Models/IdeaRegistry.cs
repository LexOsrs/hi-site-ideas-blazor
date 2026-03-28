using hi_site_ideas_blazor.Components.Ideas;

namespace hi_site_ideas_blazor.Models;

public static class IdeaRegistry
{
    public static readonly Idea[] All =
    [
        new(
            Slug: "giveaways",
            Title: "Giveaway Tracker",
            Description: "Create a giveaway on the site with a target drop, starting KC, and prize structure. A \"Send to Discord\" button creates a thread in the giveaways channel where clan members submit their KC guesses, which sync back to the site. KC updates automatically via the WOM API. Once the item is received, the site determines winners (exact, closest, 2nd/3rd closest) and allows rolling random prize draws.",
            Tags: ["pvm", "community", "discord integration"],
            Component: typeof(Giveaways)
        ),
        new(
            Slug: "bingo",
            Title: "OSRS Bingo",
            Description: "Create and manage OSRS bingo events with team boards, tile tracking, and completion history. Teams can view their board, mark tiles, and compete for bingo lines.",
            Tags: ["pvm", "community", "competition"],
            Component: typeof(Bingo)
        ),
        new(
            Slug: "example",
            Title: "Example Idea",
            Description: "A template showing how ideas are structured. Duplicate this to create new ones.",
            Tags: ["template"],
            Component: typeof(ExampleIdea)
        ),
    ];

    public static Idea? FindBySlug(string slug) =>
        All.FirstOrDefault(i => i.Slug == slug);
}
