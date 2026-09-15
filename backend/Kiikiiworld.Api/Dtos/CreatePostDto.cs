using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Kiikiiworld.Api.Dtos;

// The kinds of post that can be created through this endpoint.
[JsonConverter(typeof(JsonStringEnumConverter<CreatePostType>))]
public enum CreatePostType
{
    Quote,
    Story,
    Photo
}

public class CreatePostDto
{
    [Required]
    [EnumDataType(typeof(CreatePostType))]
    public CreatePostType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    // Optional — Photo posts are typically image-first with just a caption in Title.
    public string? Body { get; set; }
}
