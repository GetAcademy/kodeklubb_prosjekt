-- Migration 005: Add hierarchical tag fields to predefined_tags
ALTER TABLE predefined_tags
    ADD COLUMN IF NOT EXISTS parent_id UUID REFERENCES predefined_tags(id),
    ADD COLUMN IF NOT EXISTS open_for_child_suggestions BOOLEAN NOT NULL DEFAULT false;
