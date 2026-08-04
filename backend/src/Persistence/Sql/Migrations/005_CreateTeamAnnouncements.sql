-- Create team announcements for team news and announcements
CREATE TABLE IF NOT EXISTS team_announcements (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    created_by UUID NOT NULL REFERENCES users(id),
    title VARCHAR(300) NOT NULL,
    body TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INT NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS idx_team_announcements_team_id ON team_announcements(team_id);
CREATE INDEX IF NOT EXISTS idx_team_announcements_created_at ON team_announcements(created_at);
