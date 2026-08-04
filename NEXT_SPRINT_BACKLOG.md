# Next Sprint Backlog

## Goal
Deliver the next high-impact feature set for the team platform by adding team announcements, member moderation, and improved notifications.

## Priority items

### 1. Team announcements / news feed
**Why:** Current team pages are missing a content hub. Announcements make teams feel active and give a purpose to returning to the app.

**User stories**
- As a team admin, I want to create a news post for my team so members can see updates.
- As a team member, I want to view team announcements in one place.
- As a team admin, I want to edit or delete my announcements.

**Backend work**
- Add SQL schema for `team_posts` or `team_announcements`.
- Add endpoints:
  - `POST /api/discover/{teamId}/content` → create post
  - `GET /api/discover/{teamId}/content` → list posts
  - `PATCH /api/discover/{teamId}/content/{postId}` → update post
  - `DELETE /api/discover/{teamId}/content/{postId}` → delete post
- Implement authorization so only the team admin can manage posts.
- Return author metadata, created/updated timestamps, and optional body text.

**Frontend work**
- Replace placeholder content in `frontend/src/views/teams/News.vue` with a real feed.
- Add a create-post form visible to admins.
- Add edit/delete controls for admins.
- Render posts with title, body, author, and timestamp.

**Effort estimate:** 3–4 days

---

### 2. Team member moderation
**Why:** Teams need controls to manage membership and preserve quality. This is a natural extension of the existing request approval flow.

**User stories**
- As a team admin, I want to remove a member from my team.
- As a team admin, I want to promote another member to admin or transfer ownership.
- As a member, I want to see current roles and membership status.

**Backend work**
- Add role support on team members or extend the `teams` / `team_members` model.
- Add endpoints:
  - `DELETE /api/discover/{teamId}/members/{userId}` → remove member
  - `PATCH /api/discover/{teamId}/members/{userId}/role` → update member role
  - `POST /api/discover/{teamId}/transfer-admin` → transfer team admin to another member
- Add validation so only the current admin can perform moderation actions.
- Use transactions for member removal / role change.

**Frontend work**
- Update `frontend/src/views/teams/Members.vue` to show member roles and actions.
- Add remove/promote buttons for admins.
- Show action confirmation dialogs for destructive changes.

**Effort estimate:** 3–4 days

---

### 3. Notification improvements
**Why:** Notifications already exist on the backend, but the frontend experience is incomplete. Better notifications increase clarity and reduce confusion about pending requests.

**User stories**
- As a user, I want to see pending join approvals and updates in one notification pane.
- As a user, I want to mark notifications as read.
- As a user, I want notification items to link directly to the relevant team or request.

**Backend work**
- If needed, extend the notification model with `read` and `url` fields.
- Add endpoint:
  - `GET /api/discover/notifications` → list notifications
  - `PATCH /api/discover/notifications/{notificationId}/read` → mark read
- Ensure request/approval activity is surfaced consistently.

**Frontend work**
- Create or extend a notifications component/page.
- Fetch `/api/discover/notifications?discordId=...` from the current user context.
- Display separate sections for pending approvals and own updates.
- Add “mark as read” control and active links.

**Effort estimate:** 2–3 days

---

## Secondary items

### 4. Search and recommendation enhancements
**Why:** Search by team name and tags makes it easier to discover teams that match user interests.

**Backend work**
- Add search query support to `GET /api/discover/available`.
- Add optional query parameters like `?q=...` and `?tag=...`.
- Optionally add a recommended-teams endpoint using user tags.

**Frontend work**
- Add search controls to the discover page.
- Display results with tag badges and join status.

**Effort estimate:** 2–3 days

---

### 5. Developer handbook docs
**Why:** The code already includes transaction handling and outbox processing, but the team needs explicit docs for onboarding and maintenance.

**Tasks**
- Add a new section to `README.md` or create `DEVELOPER_GUIDE.md`.
- Document:
  - `DbSession` transaction usage for endpoint handlers
  - `TeamEventHandler` event log / outbox insertion
  - `OutboxWorker` polling and email processing
- Explain how to run locally and inspect worker behavior.

**Effort estimate:** 1 day

---

## Recommended first sprint
1. Team announcements/news
2. Team member moderation
3. Notifications UX
4. Developer handbook docs

Once those are in place, the app will have a stronger team experience and better operational transparency.

## Notes
- Existing code areas to modify:
  - `backend/src/Api/Endpoints/TeamEndpoints.cs`
  - `backend/src/Api/Endpoints/Handlers/TeamEventHandler.cs`
  - `frontend/src/views/teams/News.vue`
  - `frontend/src/views/teams/Members.vue`
  - `backend/src/Api/Endpoints/Handlers/DbSession.cs`
  - `backend/src/Api/Program.cs` (if worker or service registration changes are required)
- Keep authorization checks consistent with the current admin flow.
- Use the team event/outbox system for any changes that produce emails or side effects.
