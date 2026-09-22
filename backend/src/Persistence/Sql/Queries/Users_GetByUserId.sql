SELECT
    id AS "Id",
    discord_id AS "DiscordId",
    username AS "Username",
    email AS "Email",
    avatar_url AS "AvatarUrl",
    preferences_json AS "PreferencesJson",
    created_at AS "CreatedAt",
    updated_at AS "UpdatedAt",
    version AS "Version"
FROM users
WHERE id = @UserId;
