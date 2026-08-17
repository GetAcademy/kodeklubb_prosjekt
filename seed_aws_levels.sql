DO $$
DECLARE
  aws_id UUID;
BEGIN
  SELECT id INTO aws_id FROM predefined_tags WHERE name = 'AWS' AND parent_id IS NOT NULL LIMIT 1;

  IF aws_id IS NULL THEN
    RAISE NOTICE 'AWS tag not found — skipping.';
  ELSE
    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'EC2', 'ec2', aws_id, false, NOW()),
      (uuid_generate_v4(), 'S3', 's3', aws_id, false, NOW()),
      (uuid_generate_v4(), 'Lambda', 'lambda', aws_id, false, NOW()),
      (uuid_generate_v4(), 'RDS', 'rds', aws_id, false, NOW());
  END IF;
END $$;
