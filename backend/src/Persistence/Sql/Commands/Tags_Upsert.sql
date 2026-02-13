INSERT INTO tags (
    name,
    slug,
    description,
    created_at
)
VALUES (
    @Name,
    @Slug,
    '',
    NOW()
)
ON CONFLICT (slug)
DO UPDATE SET name = EXCLUDED.name
RETURNING id;
