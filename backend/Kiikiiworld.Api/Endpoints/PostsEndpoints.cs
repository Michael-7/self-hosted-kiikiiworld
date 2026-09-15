using Kiikiiworld.Api.Data;
using Kiikiiworld.Api.Dtos;
using Kiikiiworld.Api.Models;
using Kiikiiworld.Api.Storage;
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
                Media = p.Media.Select(m => new MediaDto { Id = m.Id, Type = m.Type, Url = m.Url, OriginalUrl = m.OriginalUrl }).ToList(),
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
                    Media = p.Media.Select(m => new MediaDto { Id = m.Id, Type = m.Type, Url = m.Url, OriginalUrl = m.OriginalUrl }).ToList(),
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
        })
        .RequireAuthorization();

        // DELETE post
        group.MapDelete("/{id}", async (int id, KiikiiContext dbContext, MediaStorage storage) =>
        {
            var mediaUrls = await dbContext.Media
                .Where(m => m.PostId == id)
                .Select(m => new { m.Url, m.OriginalUrl })
                .ToListAsync();

            // Related Media rows go with it via the cascade on the FK.
            var deleted = await dbContext.Posts
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync();

            if (deleted == 0) return Results.NotFound();

            foreach (var media in mediaUrls)
            {
                storage.TryDeleteByUrl(media.Url);
                storage.TryDeleteByUrl(media.OriginalUrl);
            }

            return Results.NoContent();
        })
        .RequireAuthorization();
    }
}
