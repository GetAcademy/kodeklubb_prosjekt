SELECT pt.id, pt.name, pt.description, pt.category
FROM user_tags ut
INNER JOIN users u ON u.id = ut.user_id
INNER JOIN predefined_tags pt ON pt.id = ut.predefined_tag_id
WHERE u.discord_id = @DiscordId
ORDER BY pt.name;
