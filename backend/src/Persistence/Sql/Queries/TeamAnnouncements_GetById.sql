SELECT
    id,
    team_id AS TeamId,
    created_by AS CreatedBy,
    title,
    body,
    created_at AS CreatedAt,
    updated_at AS UpdatedAt
FROM team_announcements
WHERE id = @AnnouncementId
  AND team_id = @TeamId;
