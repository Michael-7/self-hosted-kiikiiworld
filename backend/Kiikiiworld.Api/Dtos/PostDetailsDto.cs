namespace Kiikiiworld.Api.Models;

public class PostDetailsDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Hidden { get; set; } = false;
    public int PostTypeId { get; set; }
}
