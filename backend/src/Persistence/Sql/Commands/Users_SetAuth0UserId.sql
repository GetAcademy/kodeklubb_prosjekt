UPDATE users
SET auth0_user_id = @Auth0UserId,
    updated_at = NOW(),
    version = version + 1
WHERE id = @Id
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
