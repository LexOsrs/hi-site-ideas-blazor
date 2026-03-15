namespace hi_site_ideas_blazor.Models;

public record Idea(string Slug, string Title, string Description, string[] Tags, Type Component);
