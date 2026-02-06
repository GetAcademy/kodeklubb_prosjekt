-- SQL Transaction: Create Team with Admin, Tags, and Associations
-- Note: Replace {ADMIN_USER_ID} with actual user ID before running

BEGIN;

-- 1. Create tags
INSERT INTO tags (name, slug, description, created_at)
VALUES 
    ('Mobile', 'mobile', 'Mobile app development', NOW()),
    ('Cloud', 'cloud', 'Cloud infrastructure', NOW()),
    ('AI/ML', 'ai-ml', 'Artificial intelligence & machine learning', NOW());

-- 2. Create a team with existing admin user
INSERT INTO teams (name, description, created_by, team_admin_id, created_at, updated_at, version)
VALUES (
    'Mobile Development Team',
    'iOS and Android development',
    1,  -- Replace with actual admin user ID
    1,  -- Replace with actual admin user ID
    NOW(),
    NOW(),
    1
);

-- 3. Associate tags with the team
INSERT INTO team_tags (team_id, tag_id, created_at)
VALUES 
    ((SELECT MAX(id) FROM teams), (SELECT id FROM tags WHERE slug = 'mobile'), NOW()),
    ((SELECT MAX(id) FROM teams), (SELECT id FROM tags WHERE slug = 'cloud'), NOW());

-- 4. Add the admin user as team member with 'admin' role
INSERT INTO team_members (team_id, user_id, role, status, joined_at, updated_at, version)
VALUES (
    (SELECT MAX(id) FROM teams),
    1,  -- Replace with actual admin user ID
    'admin',
    'active',
    NOW(),
    NOW(),
    1
);

COMMIT;
