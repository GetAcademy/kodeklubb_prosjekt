UPDATE predefined_tags
SET parent_id = (SELECT id FROM predefined_tags WHERE name = 'Programming Languages' AND parent_id IS NULL LIMIT 1)
WHERE name = 'Erfaringsnivå' AND parent_id IS NULL;
