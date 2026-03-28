using Microsoft.EntityFrameworkCore;
using hi_site_ideas_blazor.Models;

namespace hi_site_ideas_blazor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Giveaway> Giveaways => Set<Giveaway>();
    public DbSet<Prize> Prizes => Set<Prize>();
    public DbSet<Guess> Guesses => Set<Guess>();
    public DbSet<BingoEvent> BingoEvents => Set<BingoEvent>();
    public DbSet<BingoTile> BingoTiles => Set<BingoTile>();
    public DbSet<BingoTeam> BingoTeams => Set<BingoTeam>();
    public DbSet<BingoTeamTile> BingoTeamTiles => Set<BingoTeamTile>();
    public DbSet<BingoTeamMember> BingoTeamMembers => Set<BingoTeamMember>();
    public DbSet<BingoSubmission> BingoSubmissions => Set<BingoSubmission>();
    public DbSet<BingoSubmissionEntry> BingoSubmissionEntries => Set<BingoSubmissionEntry>();
    public DbSet<BingoRequirementGroup> BingoRequirementGroups => Set<BingoRequirementGroup>();
    public DbSet<BingoRequirementOption> BingoRequirementOptions => Set<BingoRequirementOption>();
    public DbSet<BingoRequirement> BingoRequirements => Set<BingoRequirement>();
}

public class Member
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
}
