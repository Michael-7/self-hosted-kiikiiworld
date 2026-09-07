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
        group.MapGet("/", async (KiikiiContext dbContext) => {
            var posts = await dbContext.Posts
            .AsNoTracking()
            .Where(p => !p.Hidden)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new GetPostsDto
            {
                Id = p.Id,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Type = p.Type!.Name,
                Title = p.Title,
                Body = p.Body,
            })
            .ToListAsync();

            return Results.Ok(posts);
        });

        // GET by ID
        group.MapGet("/{id}", async (int id, KiikiiContext dbContext) =>
        {
            var post = await dbContext.Posts
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new GetPostsDto
                {
                    Id = p.Id,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    Type = p.Type!.Name,
                    Title = p.Title,
                    Body = p.Body,
                })
                .FirstOrDefaultAsync();

            return post is null ? Results.NotFound() : Results.Ok(post);
        })
        .WithName("GetPost");

        // CREATE posts
        group.MapPost("/", async (CreatePostDto dto, KiikiiContext dbContext) =>
        {
            // The enum name (e.g. "Quote") matches the seeded PostType.Name.
            var postType = await dbContext.PostTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == dto.Type.ToString());

            if (postType is null)
            {
                return Results.Problem($"Unknown post type '{dto.Type}'.");
            }

            Post post = new()
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PostTypeId = postType.Id,
                Title = dto.Title,
                Body = dto.Body,
            };

            dbContext.Add(post);
            await dbContext.SaveChangesAsync();

            PostDetailsDto returnPost = new()
            {
                Id = post.Id,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Hidden = post.Hidden,
                PostTypeId = post.PostTypeId,
            };

            return Results.Created($"/posts/{post.Id}", returnPost);
        });

        // DELETE post
        group.MapDelete("/{id}", async (int id, KiikiiContext dbContext) =>
        {
            // Related Media rows go with it via the cascade on the FK.
            var deleted = await dbContext.Posts
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync();

            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });
    }
}
