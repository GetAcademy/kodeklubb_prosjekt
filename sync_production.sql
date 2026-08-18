-- 1. Schema: add level_tag_id columns
ALTER TABLE team_tags ADD COLUMN IF NOT EXISTS level_tag_id UUID REFERENCES predefined_tags(id) ON DELETE SET NULL;
ALTER TABLE user_tags ADD COLUMN IF NOT EXISTS level_tag_id UUID REFERENCES predefined_tags(id) ON DELETE SET NULL;

-- 2. Seed: Geografi -> Norge -> Fylker
DO $$
DECLARE
  geografi_id UUID;
  norge_id UUID;
BEGIN
  SELECT id INTO geografi_id FROM predefined_tags WHERE name = 'Geografi' AND parent_id IS NULL;
  IF geografi_id IS NULL THEN
    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES (uuid_generate_v4(), 'Geografi', 'geografi', NULL, true, NOW())
    RETURNING id INTO geografi_id;

    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES (uuid_generate_v4(), 'Norge', 'norge', geografi_id, true, NOW())
    RETURNING id INTO norge_id;

    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'Oslo', 'oslo', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Rogaland', 'rogaland', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Møre og Romsdal', 'more-og-romsdal', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Nordland', 'nordland', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Østfold', 'ostfold', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Akershus', 'akershus', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Buskerud', 'buskerud', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Innlandet', 'innlandet', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Vestfold', 'vestfold', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Telemark', 'telemark', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Agder', 'agder', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Vestland', 'vestland', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Trøndelag', 'trondelag', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Troms', 'troms', norge_id, false, NOW()),
      (uuid_generate_v4(), 'Finnmark', 'finnmark', norge_id, false, NOW());
  END IF;
END $$;

-- 3. Seed: Cloud -> AWS -> services
DO $$
DECLARE
  aws_id UUID;
BEGIN
  SELECT id INTO aws_id FROM predefined_tags WHERE name = 'AWS' AND parent_id IS NOT NULL LIMIT 1;
  IF aws_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM predefined_tags WHERE name = 'EC2' AND parent_id = aws_id) THEN
    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'EC2', 'ec2', aws_id, false, NOW()),
      (uuid_generate_v4(), 'S3', 's3', aws_id, false, NOW()),
      (uuid_generate_v4(), 'Lambda', 'lambda', aws_id, false, NOW()),
      (uuid_generate_v4(), 'RDS', 'rds', aws_id, false, NOW());
  END IF;
END $$;

-- 4. Seed: Docker + React sub-levels
DO $$
DECLARE
  docker_id UUID;
  react_id UUID;
BEGIN
  SELECT id INTO docker_id FROM predefined_tags WHERE name = 'Docker' AND parent_id IS NOT NULL LIMIT 1;
  SELECT id INTO react_id FROM predefined_tags WHERE name = 'React' AND parent_id IS NOT NULL LIMIT 1;

  IF docker_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM predefined_tags WHERE name = 'Docker Compose' AND parent_id = docker_id) THEN
    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'Docker Compose', 'docker-compose', docker_id, false, NOW()),
      (uuid_generate_v4(), 'Dockerfile', 'dockerfile', docker_id, false, NOW()),
      (uuid_generate_v4(), 'Docker Swarm', 'docker-swarm', docker_id, false, NOW());
  END IF;

  IF react_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM predefined_tags WHERE name = 'Redux' AND parent_id = react_id) THEN
    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'Redux', 'redux', react_id, false, NOW()),
      (uuid_generate_v4(), 'Next.js', 'nextjs', react_id, false, NOW()),
      (uuid_generate_v4(), 'React Native', 'react-native', react_id, false, NOW());
  END IF;
END $$;

-- 5. Seed: Erfaringsniva, nested under Programming Languages
DO $$
DECLARE
  niva_id UUID;
  proglang_id UUID;
BEGIN
  SELECT id INTO niva_id FROM predefined_tags WHERE name = 'Erfaringsniva';
  IF niva_id IS NULL THEN
    SELECT id INTO proglang_id FROM predefined_tags WHERE name = 'Programming Languages' AND parent_id IS NULL LIMIT 1;

    INSERT INTO predefined_tags (id, name, slug, description, parent_id, open_for_child_suggestions, created_at)
    VALUES (uuid_generate_v4(), 'Erfaringsniva', 'erfaringsniva', 'Utviklerens erfaringsniva', proglang_id, true, NOW())
    RETURNING id INTO niva_id;

    INSERT INTO predefined_tags (id, name, slug, description, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'Junior (Beginner / Entry-Level)', 'junior', 'Learns basic code rules, writes simple scripts, and needs daily help or code checks from others.', niva_id, false, NOW()),
      (uuid_generate_v4(), 'Mid-Level (Intermediate)', 'mid-level', 'Works on tasks alone, fixes most normal bugs, and understands data structures well.', niva_id, false, NOW()),
      (uuid_generate_v4(), 'Senior (Expert)', 'senior', 'Designs large systems, guides team choices, and teaches junior coders.', niva_id, false, NOW()),
      (uuid_generate_v4(), 'Lead / Principal', 'lead-principal', 'Manages entire technical projects, sets company code rules, and plans long-term software goals.', niva_id, false, NOW());
  END IF;
END $$;
