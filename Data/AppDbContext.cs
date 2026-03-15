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
}

public class Member
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
}
