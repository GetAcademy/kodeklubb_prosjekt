SELECT tt.team_id AS "TeamId",
       pt.name    AS "TagName"
FROM team_tags tt
JOIN predefined_tags pt ON pt.id = tt.predefined_tag_id
ORDER BY tt.team_id, pt.name;