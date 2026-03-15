using Microsoft.EntityFrameworkCore;

namespace hi_site_ideas_blazor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
}

public class Member
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
}
