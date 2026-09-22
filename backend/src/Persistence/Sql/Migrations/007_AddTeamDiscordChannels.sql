ALTER TABLE teams
    ADD COLUMN IF NOT EXISTS discord_team_role_id TEXT,
    ADD COLUMN IF NOT EXISTS discord_text_channel_id TEXT,
    ADD COLUMN IF NOT EXISTS discord_voice_channel_id TEXT;
