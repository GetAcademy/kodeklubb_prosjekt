SELECT 
    id, 
    discord_id AS DiscordId, 
    username, 
    email, 
    avatar_url AS AvatarUrl,
    preferences_json AS PreferencesJson, 
    created_at AS CreatedAt,
    updated_at AS UpdatedAt, 
    version
FROM users
WHERE discord_id = @DiscordId;
