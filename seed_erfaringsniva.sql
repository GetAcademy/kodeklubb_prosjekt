DO $$
DECLARE
  niva_id UUID;
BEGIN
  INSERT INTO predefined_tags (id, name, slug, description, parent_id, open_for_child_suggestions, created_at)
  VALUES (uuid_generate_v4(), 'Erfaringsnivå', 'erfaringsniva', 'Utviklerens erfaringsnivå', NULL, true, NOW())
  RETURNING id INTO niva_id;

  INSERT INTO predefined_tags (id, name, slug, description, parent_id, open_for_child_suggestions, created_at)
  VALUES
    (uuid_generate_v4(), 'Junior (Beginner / Entry-Level)', 'junior', 'Learns basic code rules, writes simple scripts, and needs daily help or code checks from others.', niva_id, false, NOW()),
    (uuid_generate_v4(), 'Mid-Level (Intermediate)', 'mid-level', 'Works on tasks alone, fixes most normal bugs, and understands data structures well.', niva_id, false, NOW()),
    (uuid_generate_v4(), 'Senior (Expert)', 'senior', 'Designs large systems, guides team choices, and teaches junior coders.', niva_id, false, NOW()),
    (uuid_generate_v4(), 'Lead / Principal', 'lead-principal', 'Manages entire technical projects, sets company code rules, and plans long-term software goals.', niva_id, false, NOW());
END $$;
