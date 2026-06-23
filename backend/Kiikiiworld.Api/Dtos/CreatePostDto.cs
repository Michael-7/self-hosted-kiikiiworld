using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kiikiiworld.Api.Dtos;

// The kinds of post that can be created through this endpoint.
// Visual media is intentionally omitted for now.
[JsonConverter(typeof(JsonStringEnumConverter<CreatePostKind>))]
public enum CreatePostKind
{
    Quote,
    Story
}

public class CreatePostDto
{
    [Required]
    [EnumDataType(typeof(CreatePostKind))]
    public CreatePostKind Kind { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    public required string Body { get; set; }
}
