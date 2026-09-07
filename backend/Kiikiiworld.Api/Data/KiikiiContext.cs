using Kiikiiworld.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Kiikiiworld.Api.Data;

public class KiikiiContext(DbContextOptions<KiikiiContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostType> PostTypes => Set<PostType>();
    public DbSet<Media> Media => Set<Media>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Media>()
            .Property(m => m.Type)
            .HasConversion<string>();
    }
}
