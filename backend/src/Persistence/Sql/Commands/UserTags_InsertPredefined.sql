INSERT INTO user_tags (user_id, predefined_tag_id, created_at)
VALUES (@UserId, @PredefinedTagId, NOW())
ON CONFLICT (user_id, predefined_tag_id) DO NOTHING;
