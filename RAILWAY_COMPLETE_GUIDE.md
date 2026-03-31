# Railway Deployment - Complete Guide From Zero

## PHASE 1: Prepare Your Code (5 minutes)

### Step 1.1: Push Your Code to GitHub

Open Terminal/PowerShell:

```bash
cd c:\KodeKlub\kodeklubb_prosjekt

# Check status
git status

# Add all changes
git add .

# Commit
git commit -m "Ready for Railway deployment"

# Push to GitHub
git push origin main
```

**You should see:**
```
Everything up-to-date
```

### Step 1.2: Verify on GitHub

1. Open browser → GitHub.com
2. Go to your `kodeklubb_prosjekt` repository
3. Click **"Code"** tab
4. Look for:
   - `backend/src/Dockerfile` ✓
   - `frontend/Dockerfile` ✓
   - Latest commit shows "Ready for Railway deployment" ✓

✅ **Code is ready!**

---

## PHASE 2: Create Railway Account (2 minutes)

### Step 2.1: Sign Up

1. Go to https://railway.app
2. Click **"Sign Up"** (top right)
3. Choose **"GitHub"** to sign up
4. Authorize Railway to access GitHub
5. Confirm your email

✅ **Account created!**

---

## PHASE 3: Create Railway Project (3 minutes)

### Step 3.1: New Project

1. After login, you're in **Railway Dashboard**
2. Click **"New Project"** (large button in center)
3. A menu appears with options

---

## PHASE 4: Deploy Backend API Service

### Step 4.1: Start Backend Deployment

1. In the "New Project" menu, click **"GitHub Repo"**
2. A list of your repositories appears
3. **Click on `kodeklubb_prosjekt`**
4. Railway starts deploying...

⏳ **Wait - This is building the Backend API**

---

### Step 4.2: Configure Backend (While Building)

Once it starts building:

1. **A service appears** - it might say "Building" or "Deploying"
2. Click on the service name to open **Settings Panel**

**In the Settings Panel (on the right):**

1. **Click the Name** (top) and change it to: `api` (or keep it)
2. Click **⚙️ Settings** tab

**Find these fields and update:**

| Field | Value |
|-------|-------|
| **Root Directory** | `backend/src` |
| **Dockerfile** | `Dockerfile` |
| **Build Command** | (leave empty) |

3. **Scroll down** and click **"Save"**

✅ **Backend configuration set!**

---

### Step 4.3: Wait for Backend Build

- [ ] Watch the **Logs** tab
- [ ] Should see: `dotnet restore`, `dotnet build`, `dotnet publish`
- [ ] Wait 5-10 minutes for build to complete
- [ ] Status should change from "Building" to "Running"

✅ **Backend is running!**

---

## PHASE 5: Add PostgreSQL Database

### Step 5.1: Create Database Service

1. **In Railway Dashboard** (project view)
2. Click **"New"** button (usually top right, or bottom)
3. Select **"PostgreSQL"** from the menu
4. Wait ~30 seconds

✅ **Database created!**

---

## PHASE 6: Add Frontend Service

### Step 6.1: Start Frontend Deployment

1. Click **"New"** button in Railway Dashboard
2. Select **"GitHub Repo"**
3. **Click `kodeklubb_prosjekt`** again
4. Railway starts deploying the frontend

⏳ **Wait - Frontend is building**

---

### Step 6.2: Configure Frontend (While Building)

1. Click on the frontend service to open **Settings Panel**
2. Click **⚙️ Settings** tab

**Find these fields and update:**

| Field | Value |
|-------|-------|
| **Root Directory** | `frontend` |
| **Dockerfile** | `Dockerfile` |
| **Build Command** | (leave empty) |

3. Click **"Save"**

✅ **Frontend configuration set!**

---

### Step 6.3: Wait for Frontend Build

- [ ] Watch the **Logs** tab
- [ ] Should see: `npm ci`, `npm run build`, `npm install -g serve`
- [ ] Wait 5-10 minutes
- [ ] Status should change to "Running"

✅ **Frontend is running!**

---

## PHASE 7: Add Environment Variables

### Step 7.1: Configure Backend Variables

**In Railway Dashboard:**

1. Click on the **`api`** service (Backend)
2. Click **"Variables"** tab (middle section)
3. Click **"New Variable"** button

**Add each variable by filling the form:**

#### Variable 1:
- **Name**: `ASPNETCORE_ENVIRONMENT`
- **Value**: `Production`
- Click "Add"

#### Variable 2:
- **Name**: `ConnectionStrings__DefaultConnection`
- **Value**: Click the blue **"Reference"** button
  - Select: `${{Postgres.DATABASE_URL}}`
- Click "Add"

#### Variable 3:
- **Name**: `Discord__ClientId`
- **Value**: Your Discord Client ID (from Discord Developer Portal)
- Click "Add"

#### Variable 4:
- **Name**: `Discord__ClientSecret`
- **Value**: Your Discord Client Secret
- Click "Add"

#### Variable 5:
- **Name**: `Discord__RedirectUri`
- **Value**: `https://your-backend-domain.com/auth/discord/callback`
- (You'll get the domain in next step, for now type: `http://localhost:5154/auth/discord/callback`)
- Click "Add"

#### Variable 6:
- **Name**: `Discord__FrontendRedirectUri`
- **Value**: `https://your-frontend-domain.com`
- (Update after getting domain)
- Click "Add"

#### Variable 7:
- **Name**: `AllowedOrigins`
- **Value**: `https://your-frontend-domain.com`
- (Update after getting domain)
- Click "Add"

#### Variable 8:
- **Name**: `RESEND_API_KEY`
- **Value**: Your Resend API key (if you have one, otherwise skip)
- Click "Add"

#### Variable 9:
- **Name**: `RESEND_FROM_EMAIL`
- **Value**: `updates@updates.getacademy.no`
- Click "Add"

✅ **Backend variables configured!**

---

### Step 7.2: Configure Frontend Variables

**In Railway Dashboard:**

1. Click on the **Frontend** service
2. Click **"Variables"** tab
3. Click **"New Variable"** button

**Add variables:**

#### Variable 1:
- **Name**: `VITE_BASE_API`
- **Value**: `https://your-backend-domain.com`
- (Update after getting domain)
- Click "Add"

#### Variable 2:
- **Name**: `VITE_LOGIN_API`
- **Value**: `/auth/discord/login`
- Click "Add"

✅ **Frontend variables configured!**

---

## PHASE 8: Get Public Domains

### Step 8.1: Get Backend Domain

**In Railway Dashboard:**

1. Click on **Backend API** service
2. Look for **"Domains"** section (middle area)
3. Should show: `[Generating...]` or a domain URL
4. If it says "Generating", wait a moment, then refresh
5. Once generated, you'll see: `https://api-xxxxx.up.railway.app`
6. **COPY THIS URL**

✅ **Backend domain: `https://api-xxxxx.up.railway.app`**

---

### Step 8.2: Get Frontend Domain

**In Railway Dashboard:**

1. Click on **Frontend** service
2. Look for **"Domains"** section
3. Once generated, you'll see: `https://kodeklubb-frontend-xxxxx.up.railway.app`
4. **COPY THIS URL**

✅ **Frontend domain: `https://kodeklubb-frontend-xxxxx.up.railway.app`**

---

## PHASE 9: Update Environment Variables with Real Domains

### Step 9.1: Update Backend Variables

**In Railway Dashboard:**

1. Click on **Backend API** service
2. Click **"Variables"** tab
3. You'll see all your variables listed

**Click the edit (✏️) icon next to these and update:**

#### Update:
- **Name**: `Discord__RedirectUri`
- **New Value**: `https://api-xxxxx.up.railway.app/auth/discord/callback`

#### Update:
- **Name**: `Discord__FrontendRedirectUri`
- **New Value**: `https://kodeklubb-frontend-xxxxx.up.railway.app`

#### Update:
- **Name**: `AllowedOrigins`
- **New Value**: `https://kodeklubb-frontend-xxxxx.up.railway.app`

4. Click "Save" after each change

✅ **Backend variables updated!**

---

### Step 9.2: Update Frontend Variables

**In Railway Dashboard:**

1. Click on **Frontend** service
2. Click **"Variables"** tab
3. Click edit (✏️) next to:
   - **Name**: `VITE_BASE_API`
   - **New Value**: `https://api-xxxxx.up.railway.app`
4. Click "Save"

✅ **Frontend variables updated!**

**Services will Auto-redeploy!** Wait 1-2 minutes for them to restart with new variables.

---

## PHASE 10: Configure Discord OAuth

### Step 10.1: Update Discord Developer Portal

1. Open https://discord.com/developers/applications
2. **Log in** to Discord account
3. Click on your application
4. On **left menu**, click **"OAuth2"**
5. Click **"Redirects"** (in the left menu under OAuth2)
6. Click **"Add Another"** button
7. In the new field, paste:
   ```
   https://api-xxxxx.up.railway.app/auth/discord/callback
   ```
   (Replace `api-xxxxx.up.railway.app` with your actual backend domain)
8. Click **"Save Changes"** (bottom button)

✅ **Discord OAuth configured!**

---

## PHASE 11: Test Your Deployment

### Step 11.1: Check Service Status

**In Railway Dashboard:**

1. You should see **3 services**: API, Frontend, PostgreSQL
2. All should have **green "Running"** status
3. If any shows "Crashed", click it and check **Logs** tab for errors

### Step 11.2: Test Frontend

1. Open browser
2. Go to: `https://kodeklubb-frontend-xxxxx.up.railway.app`
3. You should see your **login page**

✅ **Frontend is working!**

### Step 11.3: Test Discord Login

1. On the frontend, click **"Discord Login"** button
2. You should be **redirected to Discord** (Discord OAuth page)
3. Click **"Authorize"**
4. You should be **logged into your app**

✅ **Discord OAuth is working!**

---

## ✅ DONE! 🎉

Your kodeklubb project is now **live on Railway**!

**Your app is running at:**
- **Frontend**: `https://kodeklubb-frontend-xxxxx.up.railway.app`
- **Backend API**: `https://api-xxxxx.up.railway.app`
- **Database**: PostgreSQL (managed by Railway)

---

## Troubleshooting

### "Service shows Crashed"
1. Click the service
2. Click **"Logs"** tab
3. Scroll to bottom to see error
4. Common fixes:
   - Missing environment variables
   - Wrong Discord credentials
   - Database connection error

### "Frontend shows blank page"
1. Open browser console (F12)
2. Check for errors
3. Usually means `VITE_BASE_API` is wrong

### "Discord login shows error"
1. Check Discord redirect URI matches exactly:
   - Should be: `https://api-xxxxx.up.railway.app/auth/discord/callback`
2. Check `Discord__RedirectUri` in backend variables matches

### "Cannot connect to database"
1. Check `ConnectionStrings__DefaultConnection` uses `${{Postgres.DATABASE_URL}}`
2. PostgreSQL service should be "Running"

---

## Next Steps (Optional)

- [ ] Set up custom domain (Railway allows this)
- [ ] Monitor logs regularly
- [ ] Set up database backups
- [ ] Test all app features

**Need help? Check the Logs tab in each service - it will tell you what's wrong!**
