using System.Text.Json.Serialization;

namespace Api.Endpoints;

public record TeamJoinRequest(
    [property: JsonPropertyName("discordId")] string DiscordId
);
