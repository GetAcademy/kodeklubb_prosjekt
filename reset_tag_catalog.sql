DO $$
DECLARE
  anvendelse_id UUID;
  frontend_id UUID;
  geografi_id UUID;
  norge_id UUID;
  proglang_id UUID;
  faglig_id UUID;
BEGIN
  -- 1. Anvendelsesomrade
  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES (uuid_generate_v4(), 'Anvendelsesomrade', 'anvendelsesomrade', NULL, true, NOW())
  RETURNING id INTO anvendelse_id;

  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES (uuid_generate_v4(), 'Backend', 'backend', anvendelse_id, false, NOW());

  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES (uuid_generate_v4(), 'Frontend', 'frontend', anvendelse_id, true, NOW())
  RETURNING id INTO frontend_id;

  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES
    (uuid_generate_v4(), 'Smart phone app', 'smart-phone-app', frontend_id, false, NOW()),
    (uuid_generate_v4(), 'Web frontend', 'web-frontend', frontend_id, false, NOW()),
    (uuid_generate_v4(), 'Windows app', 'windows-app', frontend_id, false, NOW());

  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES (uuid_generate_v4(), 'IoT', 'iot', anvendelse_id, false, NOW());

  -- 2. Geografi
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

  -- 3. Programmeringssprak
  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES (uuid_generate_v4(), 'Programmeringssprak', 'programmeringssprak', NULL, true, NOW())
  RETURNING id INTO proglang_id;

  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES
    (uuid_generate_v4(), 'JavaScript', 'javascript', proglang_id, false, NOW()),
    (uuid_generate_v4(), 'C#', 'csharp', proglang_id, false, NOW()),
    (uuid_generate_v4(), 'TypeScript', 'typescript', proglang_id, false, NOW());

  -- 4. Faglig niva
  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES (uuid_generate_v4(), 'Faglig niva', 'faglig-niva', NULL, true, NOW())
  RETURNING id INTO faglig_id;

  INSERT INTO predefined_tags (id, name, slug, parent_id, open_for_child_suggestions, created_at)
  VALUES
    (uuid_generate_v4(), 'Erfaren', 'erfaren', faglig_id, false, NOW()),
    (uuid_generate_v4(), 'Litt erfaren', 'litt-erfaren', faglig_id, false, NOW()),
    (uuid_generate_v4(), 'Nybegynner', 'nybegynner', faglig_id, false, NOW());
END $$;
