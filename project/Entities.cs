
using Microsoft.EntityFrameworkCore;

public class Task
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<Task> Tasks { get; set; } = null!;
    
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
