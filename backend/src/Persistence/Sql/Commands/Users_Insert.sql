INSERT INTO users (
    discord_id, 
    username, 
    email, 
    avatar_url, 
    preferences_json, 
    created_at, 
    updated_at, 
    version
)
VALUES (
    @DiscordId, 
    @Username, 
    @Email, 
    @AvatarUrl, 
    @PreferencesJson::jsonb, 
    NOW(), 
    NOW(), 
    1
)
RETURNING 
    id, 
    discord_id AS DiscordId, 
    username, 
    email, 
    avatar_url AS AvatarUrl,
    preferences_json AS PreferencesJson, 
    created_at AS CreatedAt,
    updated_at AS UpdatedAt, 
    version;
