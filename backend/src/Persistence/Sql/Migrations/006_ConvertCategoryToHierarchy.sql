-- Migration 006: Convert predefined tag categories into hierarchical tag roots
-- Create a root tag for each existing category and point existing tags to them.

INSERT INTO predefined_tags (id, name, slug, description, category, parent_id, open_for_child_suggestions, created_at)
SELECT
    uuid_generate_v4(),
    category,
    regexp_replace(lower(category), '[^a-z0-9]+', '-', 'g'),
    NULL,
    NULL,
    NULL,
    TRUE,
    NOW()
FROM predefined_tags
WHERE category IS NOT NULL
GROUP BY category
ON CONFLICT (slug) DO NOTHING;

UPDATE predefined_tags AS t
SET parent_id = root.id
FROM predefined_tags AS root
WHERE t.category IS NOT NULL
  AND root.slug = regexp_replace(lower(t.category), '[^a-z0-9]+', '-', 'g')
  AND t.id <> root.id;
