namespace Kiikiiworld.Api.Dtos;

public class GetPostsDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string Type { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
}
