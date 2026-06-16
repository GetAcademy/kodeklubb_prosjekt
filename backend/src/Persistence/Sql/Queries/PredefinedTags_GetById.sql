SELECT id, name, slug, description, category, parent_id AS parentTagId, open_for_child_suggestions AS openForChildSuggestions, created_at
FROM predefined_tags
WHERE id = @Id;
