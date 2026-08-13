SELECT id,
       name,
       parent_id AS "parentId",
       open_for_child_suggestions AS "openForChildSuggestions"
FROM predefined_tags
ORDER BY name;