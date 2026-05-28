ALTER TABLE discord_user_mappings
    ADD COLUMN IF NOT EXISTS oauth_access_token TEXT,
    ADD COLUMN IF NOT EXISTS token_expires_at TIMESTAMPTZ;
