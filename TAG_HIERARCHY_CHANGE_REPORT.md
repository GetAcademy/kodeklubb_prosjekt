# Tag Hierarchy & Child Suggestion Support Report

## Overview
This report documents the recent implementation for database-driven tag hierarchy support and child-suggestion gating in the project.

## Changed Files
- `backend/src/Api/Endpoints/TeamEndpoints.cs`
- `backend/src/Core/Models/Tag.cs`
- `backend/src/Persistence/DbModels/TagEntity.cs`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetAll.sql`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetByCategory.sql`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetById.sql`
- `backend/src/Persistence/Sql/Queries/UserTags_GetPredefinedByDiscordId.sql`
- `frontend/src/components/teams/TagTree.vue`
- `frontend/src/views/profile/EditProfile.vue`
- `frontend/src/views/profile/Profile.vue`
- `frontend/src/views/teams/TeamDashboard.vue`
- `backend/src/Persistence/Sql/Migrations/005_AddTagParentAndSuggestionColumns.sql`
- `backend/src/Persistence/Sql/Migrations/006_ConvertCategoryToHierarchy.sql`

## Backend Changes

### `backend/src/Api/Endpoints/TeamEndpoints.cs`
- Replaced the old JSON-backed `/api/discover/tags/hierarchy` implementation with a database-driven endpoint.
- Added `GetTagHierarchy()` to:
  - query `predefined_tags` for `id`, `name`, `parent_id`, and `open_for_child_suggestions`
  - assemble a hierarchical tree in memory
  - return a structured response where each node includes `openForChildSuggestions` and nested `children`
- Added `BuildHierarchyNode()` to recursively build node objects.
- Added internal DTOs:
  - `TagHierarchyItem` for query mapping
  - `TagHierarchyNode` for response shape
- Added `NormalizeTagSlug()` and enhanced `EnsureTagPathExistsAsync()` to:
  - split tag paths into hierarchical segments
  - create missing tags in `predefined_tags` with parent linkage
  - enforce `open_for_child_suggestions` on parent nodes before creating child tags
- Updated `GetTeamTags()` to include hierarchical metadata from `predefined_tags`:
  - `parentTagId`
  - `openForChildSuggestions`
- Replaced legacy category-based tag insert logic in `AddTeamTags()` with hierarchical tag creation.

### `backend/src/Core/Models/Tag.cs`
- Corrected property casing from `openForChildSuggestions` to `OpenForChildSuggestions`.
- Kept `ParentTagId` to support node hierarchy.

### `backend/src/Persistence/DbModels/TagEntity.cs`
- Added `OpenForChildSuggestions` to the persistence entity.
- Added `ParentTagId` alias property mapped to `ParentId`.

### SQL Query Updates
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetAll.sql`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetByCategory.sql`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetById.sql`
- `backend/src/Persistence/Sql/Queries/UserTags_GetPredefinedByDiscordId.sql`
  - Extended select lists to return `parent_id AS parentTagId` and `open_for_child_suggestions AS openForChildSuggestions`.

### Database Migrations
- `backend/src/Persistence/Sql/Migrations/005_AddTagParentAndSuggestionColumns.sql`
  - Adds `parent_id` and `open_for_child_suggestions` columns to `predefined_tags`.
- `backend/src/Persistence/Sql/Migrations/006_ConvertCategoryToHierarchy.sql`
  - Seeds category root tags into `predefined_tags`.
  - Marks root category tags with `open_for_child_suggestions = TRUE`.

## Frontend Changes

### `frontend/src/components/teams/TagTree.vue`
- Updated tree component to consume hierarchical nodes with `openForChildSuggestions` metadata.
- Added `TreeNode` interface and stronger traversal logic for nested `children`.
- Added `canAdd()` helper to only show the add button when the current node permits child suggestions.
- Added a disabled button state: `Kan ikke legge til`.
- Added styling for `.btn-disabled`.

### `frontend/src/views/profile/EditProfile.vue`
- Extended local tag interfaces with:
  - `parentTagId?: string`
  - `openForChildSuggestions?: boolean`
- This aligns user tag payload handling with the new tag metadata.

### `frontend/src/views/profile/Profile.vue`
- Extended local tag interfaces with:
  - `parentTagId?: string`
  - `openForChildSuggestions?: boolean`

### `frontend/src/views/teams/TeamDashboard.vue`
- Added a Discord community section for team views.
- This is a UI enhancement aligned with richer team metadata display.

## Validation
- `backend/src/Api` successfully built with `dotnet build`.
- No compile errors reported in `backend/src/Api/Endpoints/TeamEndpoints.cs` or `frontend/src/components/teams/TagTree.vue`.

## Notes for Presentation
- The main functional change is shifting hierarchy logic from a static JSON file to live `predefined_tags` data.
- The backend now enforces parent tag suggestion rules during tag creation.
- The frontend now renders the tree with explicit child-creation permissions.

---
Report generated on `2026-06-16`.
