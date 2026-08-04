UPDATE team_announcements
SET title = @Title,
    body = @Body,
    updated_at = NOW(),
    version = version + 1
WHERE id = @AnnouncementId
  AND team_id = @TeamId;
