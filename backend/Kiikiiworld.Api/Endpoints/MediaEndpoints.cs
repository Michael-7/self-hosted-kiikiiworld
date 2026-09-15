using Kiikiiworld.Api.Data;
using Kiikiiworld.Api.Dtos;
using Kiikiiworld.Api.Models;
using Kiikiiworld.Api.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Kiikiiworld.Api.Endpoints;

public static class MediaEndpoints
{
    private const long MaxImageBytes = 15_000_000;

    public static void MapMediaEndpoints(this WebApplication app)
    {
        app.MapPost("/posts/{postId:int}/media", async (int postId, IFormFile file, KiikiiContext dbContext, MediaStorage storage) =>
        {
            var post = await dbContext.Posts.FindAsync(postId);
            if (post is null) return Results.NotFound();

            if (file.Length == 0) return Results.BadRequest("No file uploaded.");
            if (file.Length > MaxImageBytes) return Results.BadRequest("Image too large (max 15MB).");

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);
            buffer.Position = 0;

            Image image;
            try
            {
                image = await Image.LoadAsync(buffer);
            }
            catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
            {
                return Results.BadRequest("File isn't a readable image.");
            }

            using (image)
            {
                var id = Guid.NewGuid().ToString("N");
                var ext = image.Metadata.DecodedImageFormat?.FileExtensions.FirstOrDefault() ?? "img";

                buffer.Position = 0;
                var originalPath = Path.Combine(storage.ImageOriginalDir, $"{id}.{ext}");
                await using (var fs = File.Create(originalPath))
                    await buffer.CopyToAsync(fs);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(640, 0),
                    Mode = ResizeMode.Max,
                }));

                var optimizedFileName = $"{id}.webp";
                await image.SaveAsWebpAsync(
                    Path.Combine(storage.ImageOptimizedDir, optimizedFileName),
                    new WebpEncoder { FileFormat = WebpFileFormatType.Lossy, Quality = 80 });

                var media = new Media
                {
                    Type = MediaType.Image,
                    Title = post.Title ?? file.FileName,
                    Url = $"/media/images/optimized/{optimizedFileName}",
                    OriginalUrl = $"/media/images/original/{id}.{ext}",
                    PostId = postId,
                };

                dbContext.Media.Add(media);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/posts/{postId}", new MediaDto
                {
                    Id = media.Id,
                    Type = media.Type,
                    Url = media.Url,
                    OriginalUrl = media.OriginalUrl,
                });
            }
        })
        .RequireAuthorization()
        .DisableAntiforgery();
    }
}
