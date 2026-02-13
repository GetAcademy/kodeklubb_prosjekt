-- SQL Transaction: Create Team with Admin, Tags, and Associations
-- This script creates test team data using an existing user from the database
-- Note: Replace the user_id below with an actual UUID from your users table
-- To find user IDs: SELECT id, username, discord_id FROM users;
-- To see available tags: SELECT id, name, slug, category FROM predefined_tags;

-- Create team with a specific user as admin and associate predefined tags
DO $$
DECLARE
    admin_user_id uuid := 'REPLACE-WITH-ACTUAL-USER-ID'::uuid; -- CHANGE THIS UUID
    new_team_id uuid;
    user_exists boolean;
    tag_count int;
BEGIN
    -- Verify the user exists
    SELECT EXISTS(SELECT 1 FROM users WHERE id = admin_user_id) INTO user_exists;
    
    IF NOT user_exists THEN
        RAISE EXCEPTION 'User with ID % not found. Please check the users table and update the admin_user_id.', admin_user_id;
    END IF;

    RAISE NOTICE 'Using user ID: %', admin_user_id;

    -- Create the team
    INSERT INTO teams (name, description, location, discord_link, meeting_schedule, is_open_to_join_requests, created_by, team_admin_id, created_at, updated_at, version)
    VALUES (
        'Mobile Development Team',
        'iOS and Android development',
        'Oslo',
        'https://discord.gg/example',
        'Hver mandag kl 18:00',
        true,
        admin_user_id,
        admin_user_id,
        NOW(),
        NOW(),
        1
    )
    RETURNING id INTO new_team_id;

    RAISE NOTICE 'Team created with ID: %', new_team_id;

    -- Associate predefined tags with the team
    -- Modify the WHERE clause to select different tags
    INSERT INTO team_tags (team_id, predefined_tag_id, created_at)
    SELECT 
        new_team_id,
        pt.id,
        NOW()
    FROM predefined_tags pt
    WHERE pt.slug IN ('mobile-development', 'cloud')
    ON CONFLICT (team_id, predefined_tag_id) DO NOTHING;

    -- Count and report how many tags were added
    SELECT COUNT(*) INTO tag_count FROM team_tags WHERE team_id = new_team_id;
    RAISE NOTICE 'Associated % tags with team', tag_count;

    -- Add the admin user as team member
    INSERT INTO team_members (team_id, user_id, role, status, joined_at, updated_at, version)
    VALUES (
        new_team_id,
        admin_user_id,
        'admin',
        'active',
        NOW(),
        NOW(),
        1
    );

    RAISE NOTICE 'Team successfully created with ID: % (Admin added as member)', new_team_id;
END $$;
