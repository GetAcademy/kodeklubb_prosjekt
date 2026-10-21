INSERT INTO users (discord_id, auth0_user_id, username, created_at, updated_at, version)
VALUES (@DiscordId, @Auth0UserId, @Username, NOW(), NOW(), 1)
RETURNING
    id,
    discord_id AS DiscordId,
    auth0_user_id AS Auth0UserId,
    username,
    email,
    avatar_url AS AvatarUrl,
    preferences_json AS PreferencesJson,
    created_at AS CreatedAt,
    updated_at AS UpdatedAt,
    version;
