using System.Text.Json.Serialization;
using Kiikiiworld.Api.Models;

namespace Kiikiiworld.Api.Dtos;

public class MediaDto
{
    public int Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<MediaType>))]
    public required MediaType Type { get; set; }

    public required string Url { get; set; }
    public string? OriginalUrl { get; set; }
}
