WITH tag_check AS (
    SELECT EXISTS(SELECT 1 FROM predefined_tags WHERE id = @TagId) AS tag_exists
),
inserted AS (
    INSERT INTO team_tags (id, team_id, predefined_tag_id, level_tag_id)
    SELECT uuid_generate_v4(), @TeamId, @TagId, @LevelTagId
    FROM tag_check WHERE tag_exists
    ON CONFLICT (team_id, predefined_tag_id) DO UPDATE SET level_tag_id = EXCLUDED.level_tag_id
    RETURNING id
)
SELECT tag_check.tag_exists AS TagExists,
       (SELECT count(*) FROM inserted) > 0 AS WasInserted
FROM tag_check;