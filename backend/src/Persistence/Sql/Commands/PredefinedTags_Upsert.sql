INSERT INTO predefined_tags (id, name, slug, description, category, created_at)
VALUES (@Id, @Name, @Slug, @Description, @Category, NOW())
ON CONFLICT (slug)
DO UPDATE SET 
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    category = EXCLUDED.category
RETURNING id, name, slug, description, category, created_at;
