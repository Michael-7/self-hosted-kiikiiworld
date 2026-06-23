using Kiikiiworld.Api.Data;
using Kiikiiworld.Api.Dtos;
using Kiikiiworld.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Kiikiiworld.Api.Endpoints;

public static class PostsEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/posts");

        // GET posts
        group.MapGet("/", () => "hello world!");

        group.MapPost("/", async (CreatePostDto dto, KiikiiContext dbContext) =>
        {
            // The enum name (e.g. "Quote") matches the seeded PostType.Name.
            var postType = await dbContext.PostTypes
                .FirstOrDefaultAsync(t => t.Name == dto.Kind.ToString());

            if (postType is null)
            {
                return Results.Problem($"Unknown post type '{dto.Kind}'.");
            }

            Post post = new()
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                PostTypeId = postType.Id,
                Title = dto.Title,
                Body = dto.Body,
            };

            dbContext.Add(post);
            await dbContext.SaveChangesAsync();

            PostDetailsDto returnPost = new(
                post.Id
            );

            return Results.Created($"/posts/{post.Id}", post);
        });
    }
}
