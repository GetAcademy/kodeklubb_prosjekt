SELECT pt.id,
       pt.name,
       pt.slug,
       pt.category,
       pt.parent_id AS parentTagId,
       pt.open_for_child_suggestions AS openForChildSuggestions,
       tt.level_tag_id AS levelTagId,
       lvl.name AS levelName
FROM team_tags tt
JOIN predefined_tags pt ON pt.id = tt.predefined_tag_id
LEFT JOIN predefined_tags lvl ON lvl.id = tt.level_tag_id
WHERE tt.team_id = @TeamId
ORDER BY pt.name;