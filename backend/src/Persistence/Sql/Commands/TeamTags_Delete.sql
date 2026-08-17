DELETE FROM team_tags
WHERE team_id = @TeamId
  AND predefined_tag_id = @TagId;