-- ============================================================================
-- DISCORD INTEGRATION
-- ============================================================================

-- Add Discord fields to teams table
ALTER TABLE teams ADD COLUMN IF NOT EXISTS discord_server_id VARCHAR(100);
ALTER TABLE teams ADD COLUMN IF NOT EXISTS discord_channel_id VARCHAR(100);
ALTER TABLE teams ADD COLUMN IF NOT EXISTS discord_role_id VARCHAR(100);
ALTER TABLE teams ADD COLUMN IF NOT EXISTS discord_link VARCHAR(500);

CREATE INDEX IF NOT EXISTS idx_teams_discord_server_id ON teams(discord_server_id);
CREATE INDEX IF NOT EXISTS idx_teams_discord_channel_id ON teams(discord_channel_id);

-- Add Discord user mapping table
CREATE TABLE IF NOT EXISTS discord_user_mappings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    discord_user_id VARCHAR(100) NOT NULL,
    discord_username VARCHAR(255),
    connected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, discord_user_id)
);

CREATE INDEX IF NOT EXISTS idx_discord_user_mappings_user_id ON discord_user_mappings(user_id);
CREATE INDEX IF NOT EXISTS idx_discord_user_mappings_discord_user_id ON discord_user_mappings(discord_user_id);

-- Track Discord role assignments
CREATE TABLE IF NOT EXISTS discord_role_assignments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    discord_role_id VARCHAR(100) NOT NULL,
    assigned_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    removed_at TIMESTAMPTZ,
    UNIQUE(team_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_discord_role_assignments_team_id ON discord_role_assignments(team_id);
CREATE INDEX IF NOT EXISTS idx_discord_role_assignments_user_id ON discord_role_assignments(user_id);