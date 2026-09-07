using Kiikiiworld.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Kiikiiworld.Api.Data;

public static class DataExtensions
{
    public static void MigrateDB(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KiikiiContext>();
        dbContext.Database.Migrate();
    }

    public static void AddDb(this WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("KiikiiworldDb");
        builder.Services.AddSqlite<KiikiiContext>(
            connString,
            optionsAction: options => options.UseSeeding((context, _) =>
            {
                if (!context.Set<PostType>().Any())
                {
                    context.Set<PostType>().AddRange(
                        new PostType { Name = "Photo" },
                        new PostType { Name = "Video" },
                        new PostType { Name = "Quote" },
                        new PostType { Name = "Story" },
                        new PostType { Name = "Audio" }
                    );

                    context.SaveChanges();
                }
            })
        );
    }
}
