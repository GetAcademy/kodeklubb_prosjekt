-- This migration previously seeded a large set of predefined tags
-- (Cloud, DevOps, Backend Frameworks, Programming Languages, etc.).
-- That structure was replaced by the new 4-top-level-node design
-- (Anvendelsesomrade, Geografi, Programmeringssprak, Faglig niva).
-- Left as a no-op so this migration's filename/history stays intact,
-- but it no longer re-inserts the old categories on every restart.
SELECT 1;
