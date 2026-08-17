INSERT INTO user_tags (id, user_id, predefined_tag_id, level_tag_id)
VALUES (uuid_generate_v4(), @UserId, @PredefinedTagId, @LevelTagId)
ON CONFLICT (user_id, predefined_tag_id) DO UPDATE SET level_tag_id = EXCLUDED.level_tag_id;