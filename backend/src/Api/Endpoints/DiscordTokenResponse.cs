namespace Api.Endpoints;

public record DiscordTokenResponse(
        string access_token,
        string token_type,
        int expires_in,
        string refresh_token,
        string scope
    );