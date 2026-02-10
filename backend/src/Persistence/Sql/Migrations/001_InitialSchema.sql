-- ============================================================================
-- EXTENSIONS
-- ============================================================================

-- Enable UUID generation
-- Use uuid-ossp extension for broader PostgreSQL version compatibility
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- USERS AND ACCESS
-- ============================================================================

-- Users table - basic user info, internal IDs, Discord ID, email
-- Preferences are stored as JSON in preferences_json column
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    discord_id VARCHAR(50) NOT NULL UNIQUE,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255),
    avatar_url VARCHAR(500),
    preferences_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INT NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS idx_users_discord_id ON users(discord_id);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);

-- ============================================================================
-- TAGS AND INTERESTS
-- ============================================================================

-- Tags - reusable tags for categorization with tree structure support
CREATE TABLE IF NOT EXISTS tags (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    parent_id UUID REFERENCES tags(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL UNIQUE,
    slug VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_tags_slug ON tags(slug);
CREATE INDEX IF NOT EXISTS idx_tags_name ON tags(name);
CREATE INDEX IF NOT EXISTS idx_tags_parent_id ON tags(parent_id);

-- User interests - tags that users are interested in
CREATE TABLE IF NOT EXISTS user_tags (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, tag_id)
);

CREATE INDEX IF NOT EXISTS idx_user_tags_user_id ON user_tags(user_id);
CREATE INDEX IF NOT EXISTS idx_user_tags_tag_id ON user_tags(tag_id);

-- ============================================================================
-- TEAMS AND MEMBERSHIP
-- ============================================================================

-- Teams - coding teams with description, location, meeting schedule
CREATE TABLE IF NOT EXISTS teams (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(200) NOT NULL,
    description TEXT,
    location VARCHAR(200),
    discord_link VARCHAR(500),
    meeting_schedule TEXT, -- Beskrivelse av møtetidspunkter og frekvens
    is_open_to_join_requests BOOLEAN NOT NULL DEFAULT true,
    created_by UUID NOT NULL REFERENCES users(id),
    team_admin_id UUID NOT NULL REFERENCES users(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INT NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS idx_teams_created_by ON teams(created_by);
CREATE INDEX IF NOT EXISTS idx_teams_team_admin_id ON teams(team_admin_id);

-- Team tags - tags that describe what the team is about
CREATE TABLE IF NOT EXISTS team_tags (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(team_id, tag_id)
);

CREATE INDEX IF NOT EXISTS idx_team_tags_team_id ON team_tags(team_id);
CREATE INDEX IF NOT EXISTS idx_team_tags_tag_id ON team_tags(tag_id);

-- Team members - user ↔ team connection, roles, status
CREATE TABLE IF NOT EXISTS team_members (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'member', -- 'owner', 'admin', 'member'
    status VARCHAR(50) NOT NULL DEFAULT 'active', -- 'active', 'inactive'
    joined_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INT NOT NULL DEFAULT 1,
    UNIQUE(team_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_team_members_team_id ON team_members(team_id);
CREATE INDEX IF NOT EXISTS idx_team_members_user_id ON team_members(user_id);
CREATE INDEX IF NOT EXISTS idx_team_members_status ON team_members(status);

-- Invitations - invitations sent by team leaders
CREATE TABLE IF NOT EXISTS invitations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    team_id UUID NOT NULL REFERENCES teams(id) ON DELETE CASCADE,
    invited_user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    invited_by UUID NOT NULL REFERENCES users(id),
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'accepted', 'declined', 'expired'
    invited_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    responded_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    version INT NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS idx_invitations_team_id ON invitations(team_id);
CREATE INDEX IF NOT EXISTS idx_invitations_invited_user_id ON invitations(invited_user_id);
CREATE INDEX IF NOT EXISTS idx_invitations_status ON invitations(status);
CREATE INDEX IF NOT EXISTS idx_invitations_invited_by ON invitations(invited_by);

-- ============================================================================
-- CONTENT AND PROGRESSION
-- ============================================================================

-- Nodes - learning nodes (content) with hierarchy (parent-id)
CREATE TABLE IF NOT EXISTS nodes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    parent_id UUID REFERENCES nodes(id) ON DELETE CASCADE,
    title VARCHAR(300) NOT NULL,
    description TEXT,
    content_type VARCHAR(50) NOT NULL, -- 'lesson', 'exercise', 'project', 'quiz'
    content_url VARCHAR(1000),
    order_index INT NOT NULL DEFAULT 0,
    is_published BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INT NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS idx_nodes_parent_id ON nodes(parent_id);
CREATE INDEX IF NOT EXISTS idx_nodes_is_published ON nodes(is_published);
CREATE INDEX IF NOT EXISTS idx_nodes_order_index ON nodes(order_index);

-- Node completions - who has done what (tabell beholdes for fremtidig bruk, men ikke prioritert i MVP)
CREATE TABLE IF NOT EXISTS node_completions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    node_id UUID NOT NULL REFERENCES nodes(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    completed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    score INT,
    UNIQUE(node_id, user_id)
);

CREATE INDEX IF NOT EXISTS idx_node_completions_node_id ON node_completions(node_id);
CREATE INDEX IF NOT EXISTS idx_node_completions_user_id ON node_completions(user_id);

-- ============================================================================
-- EVENTS AND OUTBOX
-- ============================================================================

-- Event log - audit log for all domain events
CREATE TABLE IF NOT EXISTS event_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_type VARCHAR(200) NOT NULL,
    aggregate_id VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    event_data JSONB NOT NULL,
    user_id UUID REFERENCES users(id),
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    metadata JSONB
);

CREATE INDEX IF NOT EXISTS idx_event_log_aggregate_id ON event_log(aggregate_id);
CREATE INDEX IF NOT EXISTS idx_event_log_aggregate_type ON event_log(aggregate_type);
CREATE INDEX IF NOT EXISTS idx_event_log_event_type ON event_log(event_type);
CREATE INDEX IF NOT EXISTS idx_event_log_occurred_at ON event_log(occurred_at);
CREATE INDEX IF NOT EXISTS idx_event_log_user_id ON event_log(user_id);

-- Outbox - domain events to be processed by worker
CREATE TABLE IF NOT EXISTS outbox (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_type VARCHAR(200) NOT NULL,
    event_data JSONB NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- 'pending', 'claimed', 'processed', 'failed'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    claimed_at TIMESTAMPTZ,
    processed_at TIMESTAMPTZ,
    failed_at TIMESTAMPTZ,
    retry_count INT NOT NULL DEFAULT 0,
    max_retries INT NOT NULL DEFAULT 3,
    error_message TEXT,
    next_retry_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_outbox_status ON outbox(status);
CREATE INDEX IF NOT EXISTS idx_outbox_claimed_at ON outbox(claimed_at);
CREATE INDEX IF NOT EXISTS idx_outbox_next_retry_at ON outbox(next_retry_at);
CREATE INDEX IF NOT EXISTS idx_outbox_created_at ON outbox(created_at);
