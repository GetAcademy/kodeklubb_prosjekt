ALTER TABLE user_tags
    ADD COLUMN IF NOT EXISTS level_tag_id UUID REFERENCES predefined_tags(id) ON DELETE SET NULL;
