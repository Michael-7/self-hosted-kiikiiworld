namespace Kiikiiworld.Api.Models;

public class Post
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Hidden { get; set; } = false;

    // Post Type Connection
    public PostType? Type { get; set; }
    public int PostTypeId { get; set; }

    // Type-specific fields. Which ones are set depends on Type.
    public string? Title { get; set; }       // story & quote
    public string? Body { get; set; }        // story & quote

    // Visual Media connection (one post -> many media)
    public List<Media> Media { get; set; } = [];
}
