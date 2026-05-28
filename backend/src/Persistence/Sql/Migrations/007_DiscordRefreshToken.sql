ALTER TABLE discord_user_mappings
    ADD COLUMN IF NOT EXISTS oauth_refresh_token TEXT;
