using Kiikiiworld.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Kiikiiworld.Api.Data;

public class KiikiiContext(DbContextOptions<KiikiiContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostType> PostTypes => Set<PostType>();
    public DbSet<VisualMedia> VisualMedia => Set<VisualMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VisualMedia>()
            .Property(m => m.Type)
            .HasConversion<string>();
    }
}
