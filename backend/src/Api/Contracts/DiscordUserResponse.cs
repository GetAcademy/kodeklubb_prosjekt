using System.Text.Json.Serialization;

namespace Api.Contracts;

public record DiscordUserResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("avatar")] string? Avatar,
    [property: JsonPropertyName("discriminator")] string? Discriminator,
    [property: JsonPropertyName("global_name")] string? GlobalName
);
