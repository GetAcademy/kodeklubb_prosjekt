-- Add the new column to both tables
ALTER TABLE team_tags ADD COLUMN IF NOT EXISTS level_tag_id UUID REFERENCES predefined_tags(id) ON DELETE SET NULL;
ALTER TABLE user_tags ADD COLUMN IF NOT EXISTS level_tag_id UUID REFERENCES predefined_tags(id) ON DELETE SET NULL;

-- Consolidate the existing 4 rows on "Mobile Development APP" into 2 properly-linked rows
UPDATE team_tags
SET level_tag_id = (SELECT id FROM predefined_tags WHERE name = 'Mid-Level (Intermediate)')
WHERE team_id = (SELECT id FROM teams WHERE name = 'Mobile Development APP')
  AND predefined_tag_id = (SELECT id FROM predefined_tags WHERE name = 'MongoDB');

UPDATE team_tags
SET level_tag_id = (SELECT id FROM predefined_tags WHERE name = 'Junior (Beginner / Entry-Level)')
WHERE team_id = (SELECT id FROM teams WHERE name = 'Mobile Development APP')
  AND predefined_tag_id = (SELECT id FROM predefined_tags WHERE name = 'Azure');

-- Delete the now-redundant standalone level rows
DELETE FROM team_tags
WHERE team_id = (SELECT id FROM teams WHERE name = 'Mobile Development APP')
  AND predefined_tag_id IN (
    SELECT id FROM predefined_tags WHERE parent_id = (SELECT id FROM predefined_tags WHERE name = 'Erfaringsniva')
  );
