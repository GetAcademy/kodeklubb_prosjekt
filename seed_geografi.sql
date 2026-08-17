DO $$
DECLARE
  geografi_id UUID;
  norge_id UUID;
BEGIN
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
END $$;
