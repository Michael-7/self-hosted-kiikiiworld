using System.Text.Json.Serialization;

namespace Kiikiiworld.Api.Dtos;

// [JsonConverter(typeof(JsonStringEnumConverter<PostType>))]
// public enum PostType
// {
//     Video,
//     Quote,
//     Image,
//     Story
// }

// public class Post
// {
//     public int Id { get; set; }
//     public DateTime CreatedAt { get; set; }
//     public string AuthorId { get; set; } = default!;
//     public PostType Type { get; set; }

//     // Type-specific fields. Which ones are set depends on Type.
//     public string? VideoUrl { get; set; }    // video
//     public string? PosterUrl { get; set; }   // video
//     public string? Text { get; set; }        // quote
//     public string? Attribution { get; set; } // quote
//     public List<string>? ImageUrls { get; set; } // image
//     public string? Title { get; set; }       // story
//     public string? Body { get; set; }        // story
//     public string? Caption { get; set; }     // video, image
// }
