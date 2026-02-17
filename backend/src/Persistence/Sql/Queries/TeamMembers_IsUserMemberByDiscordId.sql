SELECT EXISTS(
    SELECT 1
    FROM team_members tm
    INNER JOIN users u ON u.id = tm.user_id
    WHERE tm.team_id = @TeamId
      AND u.discord_id = @DiscordId
);
