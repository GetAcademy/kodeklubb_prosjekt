# Railway Deployment - Step-by-Step From Zero (Step 3+)

## Prerequisites - MUST DO FIRST ✓

### 1. GitHub Repository Setup
```bash
cd c:\KodeKlub\kodeklubb_prosjekt

# Check git status
git status

# Commit all changes
git add .
git commit -m "Configure Railway deployment - ready for production"

# Push to GitHub
git push origin main
```

**Verify**: Go to GitHub.com → your repository → confirm all files are there

---

## STEP 3: Add Services on Railway

### 3.1 - Create PostgreSQL Database Service

**In Railway Dashboard:**

1. Click **"New"** button
2. Select **"PostgreSQL"** 
3. Wait for service to be created (takes ~30 seconds)

✅ **Done** - PostgreSQL is now running!

**Your will see:**
- Service name: `postgres` or `PostgreSQL`
- Status: Running (green checkmark)

---

### 3.2 - Add Backend API Service (.NET)

**In Railway Dashboard:**

1. Click **"New"** 
2. Select **"GitHub Repo"**
3. **Select your repository**: `kodeklubb_prosjekt`
4. Click "Deploy"

**Railway will start building...**

**Once deployment starts, click on the service to open settings**

**In the service details panel:**

#### A. Update Service Settings
1. Click on the service name (top of panel)
2. Rename to: `kodeklubb-api`
3. Click the **⚙️ Settings** tab

#### B. Configure Root Directory & Dockerfile
1. Find **"Root Directory"** field
2. Set value to: `backend/src`
3. Find **"Dockerfile"** field  
4. Set value to: `Dockerfile`
5. Find **"Build Command"** field
6. Leave empty (uses Dockerfile)

#### C. Add Environment Variables
Click **"Variables"** tab, then click **"New Variable"**

Add these variables ONE BY ONE:

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__DefaultConnection` | Click the "Reference" button → select `Postgres` → select `DATABASE_URL` |
| `Discord__ClientId` | Paste your Discord Client ID |
| `Discord__ClientSecret` | Paste your Discord Client Secret |
| `Discord__RedirectUri` | Leave empty for now - update after getting domain |
| `Discord__FrontendRedirectUri` | Leave empty for now - update after getting domain |
| `AllowedOrigins` | Leave empty for now - update after getting domain |
| `RESEND_API_KEY` | (Optional) Your Resend API key |
| `RESEND_FROM_EMAIL` | `updates@updates.getacademy.no` |

**For the `ConnectionStrings__DefaultConnection` variable:**
- Click the blue **"Reference"** link
- Select **`${{Postgres.DATABASE_URL}}`** from the dropdown
- This auto-connects to your database!

#### D. Deploy Backend
1. Scroll to top
2. Click **"Deploy"** button
3. Wait for deployment to complete
4. Check logs for: `Application started`

✅ **Backend is deployed!**

---

### 3.3 - Add Frontend Service (Vue.js)

**In Railway Dashboard:**

1. Click **"New"** 
2. Select **"GitHub Repo"**
3. **Select your repository**: `kodeklubb_prosjekt`
4. Click "Deploy"

**Once deployment starts, click on the service to open settings**

**In the service details panel:**

#### A. Update Service Settings
1. Click on the service name (top of panel)
2. Rename to: `kodeklubb-frontend`
3. Click the **⚙️ Settings** tab

#### B. Configure Root Directory & Dockerfile
1. Find **"Root Directory"** field
2. Set value to: `frontend`
3. Find **"Dockerfile"** field  
4. Set value to: `Dockerfile`
5. Find **"Build Command"** field
6. Leave empty (uses Dockerfile)

#### C. Add Environment Variables
Click **"Variables"** tab, then click **"New Variable"**

Add these variables:

| Variable | Value |
|----------|-------|
| `VITE_BASE_API` | Leave empty for now - update after backend domain |
| `VITE_LOGIN_API` | `/auth/discord/login` |

#### D. Deploy Frontend
1. Click **"Deploy"** button
2. Wait for deployment to complete

✅ **Frontend is deployed!**

---

## STEP 4: Generate Domains for Each Service

### 4.1 - Get Backend API Domain

**In Railway Dashboard:**

1. Click on **`kodeklubb-api`** service
2. Look for **"Domains"** section (middle panel)
3. Click **"Generate Domain"**
4. You'll see: `https://kodeklubb-api-******.up.railway.app`
5. **COPY THIS URL** - you'll need it!

### 4.2 - Get Frontend Domain

**In Railway Dashboard:**

1. Click on **`kodeklubb-frontend`** service
2. Look for **"Domains"** section
3. Click **"Generate Domain"**
4. You'll see: `https://kodeklubb-frontend-******.up.railway.app`
5. **COPY THIS URL** - you'll need it!

---

## STEP 5: Update Environment Variables with Real Domains

### 5.1 - Update Backend Environment Variables

**In Railway Dashboard:**

1. Click on **`kodeklubb-api`** service
2. Click **"Variables"** tab
3. Edit these variables with your actual domains:

| Variable | New Value |
|----------|-----------|
| `Discord__RedirectUri` | `https://kodeklubb-api-******.up.railway.app/auth/discord/callback` |
| `Discord__FrontendRedirectUri` | `https://kodeklubb-frontend-******.up.railway.app` |
| `AllowedOrigins` | `https://kodeklubb-frontend-******.up.railway.app` |

**Click the edit icon (✏️) next to each variable to update**

### 5.2 - Update Frontend Environment Variables

**In Railway Dashboard:**

1. Click on **`kodeklubb-frontend`** service
2. Click **"Variables"** tab
3. Edit this variable:

| Variable | New Value |
|----------|-----------|
| `VITE_BASE_API` | `https://kodeklubb-api-******.up.railway.app` |

**After updating, services will Auto-redeploy!**

---

## STEP 6: Configure Discord OAuth

### 6.1 - Update Discord Developer Portal

1. Go to: https://discord.com/developers/applications
2. Select your application
3. Click **"OAuth2"** in left menu
4. Click **"Redirects"** 
5. Click **"Add Another"**
6. Paste your backend domain: 
   ```
   https://kodeklubb-api-******.up.railway.app/auth/discord/callback
   ```
7. Click **"Save Changes"**

✅ **Discord is configured!**

---

## STEP 7: Test Your Deployment

### 7.1 - Check Services Status

**In Railway Dashboard:**

- [ ] All three services show **"Running"** status (green checkmark)
- [ ] No services show "Crashed" or "Building"

### 7.2 - Check Backend Logs

1. Click on **`kodeklubb-api`** service
2. Click **"Logs"** tab
3. Look for message:
   ```
   info: Microsoft.Hosting.Lifetime[14]
       Now listening on: http://[::]:8080
   info: Microsoft.Hosting.Lifetime[0]
       Application started.
   ```

✅ **Backend is running!**

### 7.3 - Test Frontend URL

1. Open browser
2. Go to: `https://kodeklubb-frontend-******.up.railway.app`
3. You should see your login page

✅ **Frontend is running!**

### 7.4 - Test Discord Login Flow

1. On frontend, click **"Discord Login"** button
2. You should be redirected to:
   ```
   https://discord.com/oauth2/authorize?...
   ```
3. Click **"Authorize"**
4. You should be logged in on your app

✅ **Discord OAuth is working!**

---

## Troubleshooting

### Service shows "Crashed" after deployment

**Do this:**

1. Click on the service
2. Click **"Logs"** tab
3. Scroll to bottom and look for error messages
4. Common errors:
   - `ConnectionString not configured` → Check database variable
   - `Discord credentials invalid` → Check Discord__ClientId/Secret
   - `Port already in use` → Wait 1-2 minutes and redeploy

### Frontend shows blank page

1. Open browser console (F12 → Console tab)
2. Look for errors like:
   - `VITE_BASE_API` not set → Update frontend variables
   - `CORS error` → Update `AllowedOrigins` in backend

### Discord redirect shows error

1. Check Discord Portal redirect URI exactly matches:
   ```
   https://kodeklubb-api-******.up.railway.app/auth/discord/callback
   ```
2. Check `Discord__RedirectUri` in backend variables matches exactly

### Database connection failing

1. Check logs: `Click kodeklubb-api → Logs`
2. Look for: `Database connection failed`
3. Solution: Verify `ConnectionStrings__DefaultConnection` uses `${{Postgres.DATABASE_URL}}`

---

## After Deployment - Next Steps

- [ ] Test all features (login, teams, etc.)
- [ ] Check logs regularly for errors
- [ ] Set up monitoring/alerts (Railway dashboard)
- [ ] Configure custom domain (optional)
- [ ] Set up database backups

---

## Useful Commands (Optional - Using Railway CLI)

```bash
# View all services
railway services

# View logs for specific service
railway logs --service kodeklubb-api

# View environment variables
railway variables --service kodeklubb-api

# Set new variable
railway variables set VARIABLE_NAME=value --service kodeklubb-api
```

---

**You're done! 🎉**

Your kodeklubb project is now live on Railway!
