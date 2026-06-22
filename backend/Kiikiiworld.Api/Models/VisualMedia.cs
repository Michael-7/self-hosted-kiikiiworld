namespace Kiikiiworld.Api.Models;


public class VisualMedia
{
    public int Id { get; set; }
    public required MediaType Type { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public string? ThumbnailUrl { get; set; }

    // Owning post (many media -> one post)
    public int PostId { get; set; }
    public Post? Post { get; set; }
}
