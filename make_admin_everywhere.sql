-- 1. Make swatisonawane_58285 the designated admin on every team
UPDATE teams SET team_admin_id = (SELECT id FROM users WHERE username = 'swatisonawane_58285');

-- 2. Upgrade her role to admin anywhere she's already a member
UPDATE team_members SET role = 'admin'
WHERE user_id = (SELECT id FROM users WHERE username = 'swatisonawane_58285');

-- 3. Add her as admin member on any team where she isn't a member at all yet
INSERT INTO team_members (id, team_id, user_id, role, status, joined_at)
SELECT uuid_generate_v4(), t.id, u.id, 'admin', 'active', NOW()
FROM teams t
CROSS JOIN (SELECT id FROM users WHERE username = 'swatisonawane_58285') u
WHERE NOT EXISTS (
  SELECT 1 FROM team_members tm WHERE tm.team_id = t.id AND tm.user_id = u.id
);
