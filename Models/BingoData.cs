namespace hi_site_ideas_blazor.Models;

public class BingoEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Rules { get; set; }
    public string? Prizes { get; set; }
    public int BoardSize { get; set; } = 5;
    public BingoEventStatus Status { get; set; } = BingoEventStatus.Upcoming;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<BingoTile> Tiles { get; set; } = [];
    public List<BingoTeam> Teams { get; set; } = [];
}

public enum BingoEventStatus { Upcoming, Active, Completed }

public class BingoTile
{
    public int Id { get; set; }
    public int BingoEventId { get; set; }
    public int Position { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int Points { get; set; } = 1;
    public List<BingoTeamTile> TeamTiles { get; set; } = [];
    public List<BingoRequirementGroup> RequirementGroups { get; set; } = [];

    public bool HasRequirements => RequirementGroups.Count > 0;

    /// <summary>Tile complete = every group is satisfied.</summary>
    public bool IsComplete(IEnumerable<BingoSubmission> approved) =>
        RequirementGroups.Count > 0 && RequirementGroups.All(g => g.IsSatisfied(approved));
}

public class BingoRequirementGroup
{
    public int Id { get; set; }
    public int BingoTileId { get; set; }
    public string Label { get; set; } = "";
    public RequirementMode Mode { get; set; } = RequirementMode.All;
    public List<BingoRequirementOption> Options { get; set; } = [];

    /// <summary>All mode: single option, all reqs met. OneOf mode: any option fully met.</summary>
    public bool IsSatisfied(IEnumerable<BingoSubmission> approved) => Mode switch
    {
        RequirementMode.All => Options.All(o => o.IsComplete(approved)),
        RequirementMode.OneOf => Options.Any(o => o.IsComplete(approved)),
        _ => false,
    };
}

public enum RequirementMode { All, OneOf }

public class BingoRequirementOption
{
    public int Id { get; set; }
    public int BingoRequirementGroupId { get; set; }
    public string Label { get; set; } = "";
    public List<BingoRequirement> Requirements { get; set; } = [];

    public bool IsComplete(IEnumerable<BingoSubmission> approved) =>
        Requirements.All(r => GetProgress(approved, r.Label) >= r.TargetCount);

    public double CompletionFraction(IEnumerable<BingoSubmission> approved)
    {
        if (Requirements.Count == 0) return 0;
        return Requirements.Average(r =>
            Math.Min(1.0, (double)GetProgress(approved, r.Label) / r.TargetCount));
    }

    public static int GetProgress(IEnumerable<BingoSubmission> approved, string label) =>
        approved.SelectMany(s => s.Entries).Where(e => e.RequirementLabel == label).Sum(e => e.Amount);
}

public class BingoRequirement
{
    public int Id { get; set; }
    public int BingoRequirementOptionId { get; set; }
    public string Label { get; set; } = "";
    public int TargetCount { get; set; } = 1;
}

public class BingoTeam
{
    public int Id { get; set; }
    public int BingoEventId { get; set; }
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#4a9eff";
    public List<BingoTeamTile> TeamTiles { get; set; } = [];
    public List<BingoTeamMember> Members { get; set; } = [];
}

public class BingoTeamMember
{
    public int Id { get; set; }
    public int BingoTeamId { get; set; }
    public string Name { get; set; } = "";
    public double ContributionScore { get; set; }
}

public class BingoTeamTile
{
    public int Id { get; set; }
    public int BingoTeamId { get; set; }
    public int BingoTileId { get; set; }
    public TileStatus Status { get; set; } = TileStatus.NotStarted;
    public string? Proof { get; set; }
    public DateTime? CompletedAt { get; set; }
    public BingoTeam Team { get; set; } = null!;
    public BingoTile Tile { get; set; } = null!;
    public List<BingoSubmission> Submissions { get; set; } = [];

    public IEnumerable<BingoSubmission> GetApprovedSubmissions() =>
        Submissions.Where(s => s.Status == SubmissionStatus.Approved);
}

public class BingoSubmission
{
    public int Id { get; set; }
    public int BingoTeamTileId { get; set; }
    public string SubmittedBy { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string? Caption { get; set; }
    public SubmissionType Type { get; set; } = SubmissionType.Progress;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public List<BingoSubmissionEntry> Entries { get; set; } = [];

    // Convenience for single-entry submissions
    public string RequirementLabel => Entries.FirstOrDefault()?.RequirementLabel ?? "";
    public int Amount => Entries.Sum(e => e.Amount);
}

public class BingoSubmissionEntry
{
    public int Id { get; set; }
    public int BingoSubmissionId { get; set; }
    public string RequirementLabel { get; set; } = "";
    public int Amount { get; set; }
}

public enum SubmissionType { Start, Progress }
public enum SubmissionStatus { Pending, Approved, Denied }
public enum TileStatus { NotStarted, InProgress, Completed }

public static class BingoConstants
{
    public static List<BingoEvent> GetSeedEvents()
    {
        var tiles = GetEventTiles();

        var navigators = new BingoTeam
        {
            Name = "NezVro's Navigators", Color = "#ef4444",
            Members = M(("Takis", 57.27), ("HumbleDonkey", 40.33), ("Angry Asura", 35.29), ("NezVro", 31.38), ("Mtgdan m8", 27.04), ("Tom Astro", 19.58), ("Z and", 17.67), ("J sax", 15.75), ("TTV ImWakeOG", 9.94)),
        };
        var omegasus = new BingoTeam
        {
            Name = "Omegasus", Color = "#3b82f6",
            Members = M(("Pirate Smee", 53.16), ("Geography", 52.51), ("Solo Russian", 46.57), ("omegadubs", 31.78), ("hey im sam", 25.7), ("IronChefOzzy", 20.63), ("Iron Dankie", 20.22), ("Luwuker", 17.91), ("GI LOG", 6.85)),
        };
        var shooters = new BingoTeam
        {
            Name = "Salty Shooters", Color = "#22c55e",
            Members = M(("Keijo Kaasu", 72.12), ("Vial of Fate", 69.27), ("Freepopp", 68.57), ("Nandii", 54.93), ("K9VS", 42.46), ("YOUTHANASlA", 36.46), ("TzTokJizzSok", 30.87), ("Lex 26", 24.98), ("w3swift", 17.77)),
        };
        var whiteLights = new BingoTeam
        {
            Name = "White Lights", Color = "#f59e0b",
            Members = M(("fortyone", 62.75), ("yuri lover", 51.34), ("Lelc", 40.32), ("whight lite", 39.77), ("Jesssy", 27.64), ("S ekiro", 21.87), ("Yeqzz", 17.67), ("iron ssgoku", 10.89), ("7 Up", 7.08)),
        };
        var planking = new BingoTeam
        {
            Name = "Rapiddly Planking", Color = "#8b5cf6",
            Members = M(("Tolony", 62.27), ("HeresALlama", 49.99), ("Tides", 38.88), ("Toxic V", 35.98), ("Terrorthepic", 29.19), ("D4RE", 25.27), ("Scoopy18 GIM", 15.75), ("Rapidd", 12.53), ("iPaul07", 11.4)),
        };
        var papis = new BingoTeam
        {
            Name = "Papi's Sloppy Droppies", Color = "#ec4899",
            Members = M(("Mvttman", 36.19), ("MousseNukkl", 32.09), ("Jackolantern", 25.94), ("Fe Jiggles", 23.82), ("Bear OG", 23.75), ("le fe weasel", 18.93), ("xBeerdo", 18.58), ("El Papicito", 18.54), ("IronDadGamer", 6.93)),
        };
        var newts = new BingoTeam
        {
            Name = "Send Newts", Color = "#06b6d4",
            Members = M(("Thuan Cena", 75.03), ("IronWr4th", 65.59), ("Toast N Bean", 44.11), ("Its Newt", 37.7), ("Papa Like", 36.89), ("Yeka", 28.14), ("GIM Rekkless", 26.34), ("Scapin", 25.19), ("2 R", 19.67)),
        };

        shooters.TeamTiles =
        [
            new() { Tile = tiles[5], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[12], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 5, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[13], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 12, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[6], Status = TileStatus.InProgress },
            new() { Tile = tiles[2], Status = TileStatus.InProgress, Submissions =
            [
                new() { SubmittedBy = "Freepopp", Type = SubmissionType.Progress, Status = SubmissionStatus.Approved, Caption = "40k scales from farming Zulrah", ImageUrl = "", ReviewedBy = "Admin", ReviewedAt = new DateTime(2025, 7, 5, 0, 0, 0, DateTimeKind.Utc), SubmittedAt = new DateTime(2025, 7, 4, 0, 0, 0, DateTimeKind.Utc), Entries = [new() { RequirementLabel = "Scales", Amount = 40000 }] },
                new() { SubmittedBy = "Nandii", Type = SubmissionType.Progress, Status = SubmissionStatus.Approved, Caption = "24k scales + magic fang (20k)", ImageUrl = "", ReviewedBy = "Admin", ReviewedAt = new DateTime(2025, 7, 12, 0, 0, 0, DateTimeKind.Utc), SubmittedAt = new DateTime(2025, 7, 11, 0, 0, 0, DateTimeKind.Utc), Entries = [new() { RequirementLabel = "Scales", Amount = 24000 }] },
            ] },
            new() { Tile = tiles[10], Status = TileStatus.InProgress, Submissions =
            [
                new() { SubmittedBy = "Nandii", Type = SubmissionType.Start, Status = SubmissionStatus.Approved, Caption = "Starting Barrows KC screenshot", ImageUrl = "https://res.cloudinary.com/dflntlsqq/image/upload/v1774304177/nandi_barrows_start_lporrl.png", ReviewedBy = "Admin", ReviewedAt = new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc), SubmittedAt = new DateTime(2025, 6, 4, 0, 0, 0, DateTimeKind.Utc) },
                new() { SubmittedBy = "Nandii", Type = SubmissionType.Progress, Status = SubmissionStatus.Approved, Caption = "Verac's plateskirt drop!", ImageUrl = "https://res.cloudinary.com/dflntlsqq/image/upload/v1774304177/nandi_barrows_item_1_l4zqjz.png", ReviewedBy = "Admin", ReviewedAt = new DateTime(2025, 6, 20, 0, 0, 0, DateTimeKind.Utc), SubmittedAt = new DateTime(2025, 6, 19, 0, 0, 0, DateTimeKind.Utc), Entries = [new() { RequirementLabel = "Verac's items", Amount = 1 }] },
                new() { SubmittedBy = "Keijo Kaasu", Type = SubmissionType.Progress, Status = SubmissionStatus.Pending, Caption = "Eclipse moon helm obtained", ImageUrl = "https://res.cloudinary.com/dflntlsqq/image/upload/v1774304176/keijo_eclipse_1_aamvc5.png", SubmittedAt = new DateTime(2025, 6, 24, 0, 0, 0, DateTimeKind.Utc), Entries = [new() { RequirementLabel = "Eclipse moon items", Amount = 1 }] },
            ] },
        ];
        newts.TeamTiles =
        [
            new() { Tile = tiles[19], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 7, 15, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[23], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 18, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[0], Status = TileStatus.InProgress },
            new() { Tile = tiles[1], Status = TileStatus.InProgress },
            new() { Tile = tiles[12], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc), Submissions =
            [
                new() { SubmittedBy = "Thuan Cena", Type = SubmissionType.Progress, Status = SubmissionStatus.Approved, Caption = "130 contracts done", ImageUrl = "", ReviewedBy = "Admin", ReviewedAt = new DateTime(2025, 7, 20, 0, 0, 0, DateTimeKind.Utc), SubmittedAt = new DateTime(2025, 7, 19, 0, 0, 0, DateTimeKind.Utc), Entries = [new() { RequirementLabel = "Contracts", Amount = 130 }] },
                new() { SubmittedBy = "IronWr4th", Type = SubmissionType.Progress, Status = SubmissionStatus.Approved, Caption = "120 contracts", ImageUrl = "", ReviewedBy = "Admin", ReviewedAt = new DateTime(2025, 7, 28, 0, 0, 0, DateTimeKind.Utc), SubmittedAt = new DateTime(2025, 7, 27, 0, 0, 0, DateTimeKind.Utc), Entries = [new() { RequirementLabel = "Contracts", Amount = 120 }] },
            ] },
        ];
        planking.TeamTiles =
        [
            new() { Tile = tiles[5], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 7, 25, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[0], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 8, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[7], Status = TileStatus.InProgress },
            new() { Tile = tiles[15], Status = TileStatus.InProgress },
        ];
        whiteLights.TeamTiles =
        [
            new() { Tile = tiles[19], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 7, 10, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[13], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 2, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[16], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 15, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[11], Status = TileStatus.InProgress },
        ];
        omegasus.TeamTiles =
        [
            new() { Tile = tiles[5], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 7, 28, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[0], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 10, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[18], Status = TileStatus.InProgress },
        ];
        navigators.TeamTiles =
        [
            new() { Tile = tiles[12], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 7, 30, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[23], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 14, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[9], Status = TileStatus.InProgress },
            new() { Tile = tiles[17], Status = TileStatus.InProgress },
        ];
        papis.TeamTiles =
        [
            new() { Tile = tiles[12], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 3, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[13], Status = TileStatus.Completed, CompletedAt = new DateTime(2025, 8, 20, 0, 0, 0, DateTimeKind.Utc) },
            new() { Tile = tiles[20], Status = TileStatus.InProgress },
        ];

        return
        [
            new()
            {
                Title = "Bingo 6",
                Rules = "Each tile is worth a different number of points based on the time it would take to efficiently complete it. Tiles that contain group content have been buffed. An additional 50 points is added to your team's total score every time you complete a row, column, or diagonal.\n\nTo submit a screenshot for an item, post in the appropriate thread underneath your team channel. A screenshot should include your full game window displaying your RuneScape username. You should have the Character Summary tab open and the event phrase visible on screen. If you are on RuneLite, you can download the Clan Events plugin or the Wise Old Man plugin to keep the phrase on your screen at all times.\n\nIt is recommended to turn on loot drop notifications and lower the minimum value. You should also take a picture of the relevant Collection Log pages before and after working on a specific tile.\n\nAny raid rewards need to be obtained inside of the respective loot chamber.\n\nWhen you receive an item, reply to your starting screenshot (or post starting and ending screenshots at the same time) and mention a @Moderator who will verify and mark it as completed for you.",
                Prizes = "Main prize pool: 1,350m gp (+ 200m donated by BigKnobb)\n\n1st place: 100m gp per team member\n2nd place: 50m gp per team member\n\n1 bond per team member to the first team to complete any row, column, or diagonal (donated by XgodofironX)\n50m gp to the captain of the winning team (donated by iFencig / Geevs Uncle)\n20m gp to the captains of the top 3 teams (donated by TekThuan / LeThuanJames)\n1 bond to the MVP of the last place team (donated by iron ssgoku / apexofirons and Omav)\n\nMVP of each team will have their entry fee of 20m gp returned.\n\nRewards can be taken as-is or converted to bonds, depending on each participant's preference.",
                BoardSize = 5,
                Status = BingoEventStatus.Active,
                StartsAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAt = new DateTime(2025, 9, 1, 1, 0, 0, DateTimeKind.Utc),
                Tiles = tiles,
                Teams = [shooters, newts, planking, whiteLights, omegasus, navigators, papis],
            },
        ];
    }

    private static string Wiki(string item) =>
        $"https://oldschool.runescape.wiki/images/{Uri.EscapeDataString(item.Replace(' ', '_'))}_detail.png";

    // All mode: single option with all requirements AND'd
    static BingoRequirementGroup All(string label, params (string Label, int Target)[] reqs) => new()
    {
        Label = label, Mode = RequirementMode.All,
        Options = [new() { Label = label, Requirements = reqs.Select(r => new BingoRequirement { Label = r.Label, TargetCount = r.Target }).ToList() }],
    };

    // OneOf mode: multiple options, pick the one that completes first
    static BingoRequirementGroup OneOf(string label, params (string OptionLabel, (string Label, int Target)[] Reqs)[] options) => new()
    {
        Label = label, Mode = RequirementMode.OneOf,
        Options = options.Select(o => new BingoRequirementOption
        {
            Label = o.OptionLabel,
            Requirements = o.Reqs.Select(r => new BingoRequirement { Label = r.Label, TargetCount = r.Target }).ToList(),
        }).ToList(),
    };

    private static List<BingoTile> GetEventTiles() =>
    [
        // 0: Slayer Trio
        new()
        {
            Position = 0, Title = "Slayer Trio", Points = 54, ImageUrl = Wiki("Brimstone ring"),
            Description = "Obtain 3 pieces of a Noxious halberd, 3 pieces of an Abyssal bludgeon, and 3 pieces of a Brimstone ring as a team. The pieces do not have to be unique.",
            RequirementGroups =
            [
                All("Requirements", ("Noxious halberd pieces", 3), ("Abyssal bludgeon pieces", 3), ("Brimstone ring pieces", 3)),
            ],
        },
        // 1: Endgame Trio
        new()
        {
            Position = 1, Title = "Endgame Trio", Points = 58, ImageUrl = Wiki("Inquisitor's great helm"),
            Description = "Obtain 1 item from the Inquisitor's armour set, 1 item from the Oathplate armour set, and 1 item from the Sunfire fanatic armour set as a team. The items cannot be worn in the same equipment slot.",
            RequirementGroups =
            [
                OneOf("Equipment slot", ("Helm slot", [("Inquisitor's great helm", 1), ("Oathplate helm", 1), ("Sunfire fanatic helm", 1)]),
                                        ("Body slot", [("Inquisitor's hauberk", 1), ("Oathplate chest", 1), ("Sunfire fanatic cuirass", 1)]),
                                        ("Legs slot", [("Inquisitor's plateskirt", 1), ("Oathplate legs", 1), ("Sunfire fanatic chausses", 1)])),
            ],
        },
        // 2: Zulrah's scales
        new()
        {
            Position = 2, Title = "Zulrah's scales", Points = 22, ImageUrl = "https://oldschool.runescape.wiki/images/Zulrah%27s_scales_detail.png",
            Description = "Obtain 100,000 Zulrah's scales as a team. Each unique obtained will count as 20,000 scales.",
            RequirementGroups = [All("Requirements", ("Scales", 100000))],
        },
        // 3: Crystal shards
        new()
        {
            Position = 3, Title = "Crystal shards", Points = 43, ImageUrl = "https://oldschool.runescape.wiki/images/Crystal_shard_detail.png",
            Description = "Obtain 1,500 Crystal shards as a team via unique crystal seeds only.",
            RequirementGroups = [All("Requirements", ("Shards", 1500))],
        },
        // 4: Torva
        new()
        {
            Position = 4, Title = "Torva", Points = 49, ImageUrl = Wiki("Torva full helm"),
            Description = "Obtain any piece of Torva and the corresponding amount of Bandosian components to repair it as a team.",
            RequirementGroups =
            [
                OneOf("Torva path", ("Helm path", [("Torva full helm (damaged)", 1), ("Bandosian components", 1)]),
                                    ("Platebody path", [("Torva platebody (damaged)", 1), ("Bandosian components", 3)]),
                                    ("Platelegs path", [("Torva platelegs (damaged)", 1), ("Bandosian components", 2)])),
            ],
        },
        // 5: Raids Trio
        new()
        {
            Position = 5, Title = "Raids Trio", Points = 222, ImageUrl = Wiki("Ancestral hat"),
            Description = "Obtain 1 item from Ancestral, 1 from Justiciar, 1 from Masori + Armadyl plates. Items cannot be the same equipment slot.",
            RequirementGroups =
            [
                OneOf("Equipment slot", ("Helm path", [("Ancestral hat", 1), ("Justiciar faceguard", 1), ("Masori mask", 1), ("Armadyl plates", 1)]),
                                        ("Body path", [("Ancestral robe top", 1), ("Justiciar chestguard", 1), ("Masori body", 1), ("Armadyl plates", 4)]),
                                        ("Legs path", [("Ancestral robe bottom", 1), ("Justiciar legguards", 1), ("Masori chaps", 1), ("Armadyl plates", 3)])),
            ],
        },
        // 6: Wilderness Wards
        new()
        {
            Position = 6, Title = "Wilderness Wards", Points = 10, ImageUrl = Wiki("Malediction ward"),
            Description = "Obtain the shards required to create a Malediction ward and an Odium ward from scratch as a team.",
            RequirementGroups = [All("Requirements", ("Malediction shards", 3), ("Odium shards", 3))],
        },
        // 7: Dragon hunter wand
        new()
        {
            Position = 7, Title = "Dragon hunter wand", Points = 16, ImageUrl = Wiki("Dragon hunter lance"),
            Description = "Obtain a Dragon hunter wand.",
            RequirementGroups = [All("Requirements", ("Dragon hunter wand", 1))],
        },
        // 8: Staff of light
        new()
        {
            Position = 8, Title = "Staff of light", Points = 27, ImageUrl = Wiki("Staff of light"),
            Description = "Obtain the items required to create a Staff of light from scratch as a team.",
            RequirementGroups = [All("Requirements", ("Staff of the dead", 1), ("Saradomin's light", 1))],
        },
        // 9: Mahogany Homes
        new()
        {
            Position = 9, Title = "Mahogany Homes", Points = 27, ImageUrl = Wiki("Mahogany plank"),
            Description = "Complete 1,500 contracts as a team.",
            RequirementGroups = [All("Requirements", ("Contracts", 1500))],
        },
        // 10: Perilous Barrows
        new()
        {
            Position = 10, Title = "Perilous Barrows", Points = 28, ImageUrl = Wiki("Dharok's greataxe"),
            Description = "Obtain 4 items from the same Barrows armour set and 4 items from the same Moons of Peril armour set as a team. Items do not have to be unique.",
            RequirementGroups =
            [
                OneOf("Barrows set", ("Ahrim's", [("Ahrim's items", 4)]),
                                     ("Dharok's", [("Dharok's items", 4)]),
                                     ("Guthan's", [("Guthan's items", 4)]),
                                     ("Karil's", [("Karil's items", 4)]),
                                     ("Torag's", [("Torag's items", 4)]),
                                     ("Verac's", [("Verac's items", 4)])),
                OneOf("Moons set", ("Eclipse moon", [("Eclipse moon items", 4)]),
                                   ("Blood moon", [("Blood moon items", 4)]),
                                   ("Blue moon", [("Blue moon items", 4)])),
            ],
        },
        // 11: Skilling Bosses
        new()
        {
            Position = 11, Title = "Skilling Bosses", Points = 36, ImageUrl = Wiki("Tome of fire"),
            Description = "Earn 2,500 rewards from Tempoross and/or Wintertodt as a team.",
            RequirementGroups = [All("Requirements", ("Rewards", 2500))],
        },
        // 12: Farming Contracts
        new()
        {
            Position = 12, Title = "Farming Contracts", Points = 125, ImageUrl = Wiki("Seed pack"),
            Description = "Complete 250 contracts as a team.",
            RequirementGroups = [All("Requirements", ("Contracts", 250))],
        },
        // 13: Conflicted Rancour
        new()
        {
            Position = 13, Title = "Conflicted Rancour", Points = 70, ImageUrl = Wiki("Amulet of rancour"),
            Description = "Obtain the items required to create Confliction gauntlets and an Amulet of rancour from scratch as a team.",
            RequirementGroups = [All("Requirements", ("Uncut onyx", 2), ("Zenyte shard", 2), ("Mokhaiotl cloth", 1), ("Araxyte fang", 1))],
        },
        // 14: Frozen tears
        new()
        {
            Position = 14, Title = "Frozen tears", Points = 36, ImageUrl = Wiki("Pendant of ates"),
            Description = "Obtain 10,000 Frozen tears as a team. Each Pendant of ates counts as 100 tears.",
            RequirementGroups = [All("Requirements", ("Tears", 10000))],
        },
        // 15: Echo boots
        new()
        {
            Position = 15, Title = "Echo boots", Points = 32, ImageUrl = Wiki("Echo boots"),
            Description = "Obtain the items required to create Echo boots from scratch as a team.",
            RequirementGroups = [All("Requirements", ("Bandos boots", 1), ("Black tourmaline core", 1), ("Echo crystal", 1))],
        },
        // 16: Mounted Heads
        new()
        {
            Position = 16, Title = "Mounted Heads", Points = 56, ImageUrl = Wiki("Vorkath's head"),
            Description = "Obtain every mountable head from bosses as a team.",
            RequirementGroups = [All("Requirements", ("Abyssal head", 1), ("Alchemical hydra heads", 1), ("KBD heads", 1), ("KQ head", 1), ("Vorkath's head", 1))],
        },
        // 17: Hunter Rumours
        new()
        {
            Position = 17, Title = "Hunter Rumours", Points = 33, ImageUrl = Wiki("Hunters' loot sack (master)"),
            Description = "Complete 500 rumours as a team.",
            RequirementGroups = [All("Requirements", ("Rumours", 500))],
        },
        // 18: Venator bow
        new()
        {
            Position = 18, Title = "Venator bow", Points = 20, ImageUrl = Wiki("Venator bow"),
            Description = "Obtain 5 Venator shards as a team.",
            RequirementGroups = [All("Requirements", ("Venator shards", 5))],
        },
        // 19: Soulreaper axe
        new()
        {
            Position = 19, Title = "Soulreaper axe", Points = 117, ImageUrl = Wiki("Soulreaper axe"),
            Description = "Obtain the items required to create a Soulreaper axe from scratch as a team.",
            RequirementGroups = [All("Requirements", ("Eye of the duke", 1), ("Leviathan's lure", 1), ("Executioner's axe head", 1), ("Siren's staff", 1))],
        },
        // 20: Giantsoul amulets
        new()
        {
            Position = 20, Title = "Giantsoul amulets", Points = 13, ImageUrl = Wiki("Giantsoul amulet"),
            Description = "Obtain 25 Giantsoul amulets as a team.",
            RequirementGroups = [All("Requirements", ("Giantsoul amulets", 25))],
        },
        // 21: Avernic treads (max)
        new()
        {
            Position = 21, Title = "Avernic treads (max)", Points = 62, ImageUrl = Wiki("Avernic treads (max)"),
            Description = "Obtain the items required to create Avernic treads (max) from scratch as a team.",
            RequirementGroups = [All("Requirements", ("Dragon boots", 1), ("Ranger boots", 1), ("Infinity boots", 1), ("Primordial crystal", 1), ("Pegasian crystal", 1), ("Eternal crystal", 1), ("Avernic treads", 1))],
        },
        // 22: Tonalztics of ralos
        new()
        {
            Position = 22, Title = "Tonalztics of ralos", Points = 33, ImageUrl = Wiki("Tonalztics of ralos"),
            Description = "Obtain a Tonalztics of ralos (uncharged). Must be claimed after completing wave 12.",
            RequirementGroups = [All("Requirements", ("Tonalztics of ralos", 1))],
        },
        // 23: Upgraded Rev. Weapon
        new()
        {
            Position = 23, Title = "Upgraded Rev. Weapon", Points = 110, ImageUrl = Wiki("Craw's bow"),
            Description = "Obtain the items required to create an upgraded revenant weapon from scratch as a team.",
            RequirementGroups =
            [
                OneOf("Revenant weapon", ("Craw's bow", [("Craw's bow (u)", 1)]),
                                         ("Thammaron's sceptre", [("Thammaron's sceptre (u)", 1)]),
                                         ("Viggora's chainmace", [("Viggora's chainmace (u)", 1)])),
                OneOf("Wilderness boss drop", ("Venenatis", [("Fangs of venenatis", 1)]),
                                              ("Vet'ion", [("Skull of vet'ion", 1)]),
                                              ("Callisto", [("Claws of callisto", 1)])),
            ],
        },
        // 24: Tormented Demons
        new()
        {
            Position = 24, Title = "Tormented Demons", Points = 25, ImageUrl = Wiki("Tormented synapse"),
            Description = "Obtain 3 Tormented synapses as a team.",
            RequirementGroups = [All("Requirements", ("Tormented synapses", 3))],
        },
    ];

    private static List<BingoTeamMember> M(params (string Name, double Score)[] members) =>
        members.Select(m => new BingoTeamMember { Name = m.Name, ContributionScore = m.Score }).ToList();
}
