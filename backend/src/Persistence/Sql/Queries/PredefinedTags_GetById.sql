SELECT id, name, slug, description, category, created_at
FROM predefined_tags
WHERE id = @Id;
