SELECT t.name
FROM user_tags ut
INNER JOIN users u ON u.id = ut.user_id
INNER JOIN tags t ON t.id = ut.tag_id
WHERE u.discord_id = @DiscordId
ORDER BY t.name;
