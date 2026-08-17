DO $$
DECLARE
  docker_id UUID;
  react_id UUID;
BEGIN
  SELECT id INTO docker_id FROM predefined_tags WHERE name = 'Docker' AND parent_id IS NOT NULL LIMIT 1;
  SELECT id INTO react_id FROM predefined_tags WHERE name = 'React' AND parent_id IS NOT NULL LIMIT 1;

  IF docker_id IS NOT NULL THEN
    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'Docker Compose', 'docker-compose', docker_id, false, NOW()),
      (uuid_generate_v4(), 'Dockerfile', 'dockerfile', docker_id, false, NOW()),
      (uuid_generate_v4(), 'Docker Swarm', 'docker-swarm', docker_id, false, NOW());
  END IF;

  IF react_id IS NOT NULL THEN
    INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
    VALUES
      (uuid_generate_v4(), 'Redux', 'redux', react_id, false, NOW()),
      (uuid_generate_v4(), 'Next.js', 'nextjs', react_id, false, NOW()),
      (uuid_generate_v4(), 'React Native', 'react-native', react_id, false, NOW());
  END IF;
END $$;
