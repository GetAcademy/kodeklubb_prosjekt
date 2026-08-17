INSERT INTO user_tags (id, user_id, predefined_tag_id)
VALUES (uuid_generate_v4(), @UserId, @PredefinedTagId)
ON CONFLICT (user_id, predefined_tag_id) DO NOTHING;
