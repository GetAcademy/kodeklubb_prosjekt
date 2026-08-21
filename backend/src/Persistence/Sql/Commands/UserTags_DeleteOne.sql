DELETE FROM user_tags
WHERE predefined_tag_id = @TagId
  AND user_id = (SELECT id FROM users WHERE discord_id = @DiscordId);