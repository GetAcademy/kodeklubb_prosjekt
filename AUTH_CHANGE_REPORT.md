# Kode Klubb - Change Report for Testing

## Summary
This report summarizes the changes made to fix the Discord OAuth login flow and support correct frontend/backend authentication behavior.

The main fix addressed:
- Discord login redirect and OAuth callback handling.
- Backend configuration loading from `.env`.
- Frontend router and login page handling for `token` and `user` query params.
- Redirecting successful login to the dashboard/profile view.

## Key Files Changed

### Backend
- `backend/src/Api/Endpoints/DiscordEndpoints.cs`
  - Fixed Discord login callback logic.
  - Added reliable configuration lookup using environment variables and app settings.
  - Ensures callback redirects back to frontend with `token` and `user` query parameters.
  - Improved error handling for OAuth token exchange and user fetch.

- `backend/src/Api/Program.cs`
  - Added ancestor-directory `.env` loading so local environment config is available.
  - Ensures the backend uses the correct Discord and database configuration values at runtime.
  - Added debug output for connection string resolution.

### Frontend
- `frontend/src/router/index.ts`
  - Handles OAuth callback query params (`token` and `user`).
  - Sets authentication state in the Pinia store during route navigation.
  - Redirects successful login from `/` to `/profile`.
  - Cleans query params from the URL after processing.

- `frontend/src/views/Index.vue`
  - Added a visible Discord login button and login page UI.
  - Keeps dashboard content hidden until authentication is confirmed.
  - Ensures proper redirection to the backend login endpoint.

## Additional Modified Files
The working tree also includes other modified files that are present in this repo state. These appear to be outside the core Discord OAuth fix but should be noted for review.

- `backend/src/Api/Endpoints/TeamEndpoints.cs`
- `backend/src/Api/appsettings.Development.json`
- `backend/src/Core/Models/Tag.cs`
- `backend/src/Persistence/DbModels/TagEntity.cs`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetAll.sql`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetByCategory.sql`
- `backend/src/Persistence/Sql/Queries/PredefinedTags_GetById.sql`
- `backend/src/Persistence/Sql/Queries/UserTags_GetPredefinedByDiscordId.sql`
- `frontend/package-lock.json`
- `frontend/src/components/teams/AddTags.vue`
- `frontend/src/components/teams/TagTree.vue`
- `frontend/src/views/profile/EditProfile.vue`
- `frontend/src/views/profile/Profile.vue`
- `frontend/src/views/teams/TeamDashboard.vue`

## Testing Checklist

1. Start the backend and frontend apps.
2. Open the frontend app at `http://localhost:3000`.
3. Click the Discord login button.
4. Confirm redirect to Discord and successful login.
5. After login, observe redirect to frontend and automatic navigation to `/profile`.
6. Verify the browser URL does NOT remain on `?error=token_exchange_failed`.
7. Confirm that the frontend shows authenticated user data and/or dashboard content.

## Notes
- The backend must use the same Discord client ID/secret and redirect URI configured in Discord Developer Portal.
- The redirect URI must exactly match `http://localhost:5154/auth/discord/callback`.
- If the login still returns `token_exchange_failed`, the Discord app credentials or redirect URI need to be corrected.

## Recommended Review Items
- Ensure `Discord__ClientId`, `Discord__ClientSecret`, and `Discord__RedirectUri` are correct in `.env`.
- Confirm the backend is restarted after any `.env` changes.
- Validate the frontend route handling after login.
