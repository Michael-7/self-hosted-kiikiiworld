using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kiikiiworld.Api.Dtos;

// The kinds of post that can be created through this endpoint.
// Visual media is intentionally omitted for now.
[JsonConverter(typeof(JsonStringEnumConverter<CreatePostType>))]
public enum CreatePostType
{
    Quote,
    Story
}

public class CreatePostDto
{
    [Required]
    [EnumDataType(typeof(CreatePostType))]
    public CreatePostType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Body { get; set; }
}
