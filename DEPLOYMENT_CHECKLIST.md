# Railway Deployment Checklist

## Pre-Deployment Setup ✓

- [x] Created `railway.json` - Railway service configuration
- [x] Created `frontend/Dockerfile` - Production-ready with multi-stage build
- [x] Updated `backend/src/Api/Program.cs` - Dynamic CORS configuration
- [x] Created `appsettings.Production.json` - Production logging configuration
- [x] Updated `.env.example` - Added all required environment variables
- [x] Created `frontend/.env.production` - Frontend production config template
- [x] Created `.railwayignore` - Files to exclude from deployment
- [x] Created deployment guides - RAILWAY_DEPLOYMENT.md and RAILWAY_QUICK_START.md

## Before You Deploy - Checklist

### GitHub Repository
- [ ] All changes committed and pushed to GitHub
- [ ] Repository is public (or Railway has access)
- [ ] Main branch contains your latest code

### Discord OAuth Setup
- [ ] You have a Discord Developer Application created
- [ ] You have the `Client ID`
- [ ] You have the `Client Secret`
- [ ] Note: Redirect URI will be added during Railway deployment

### Resend Email Service (Optional)
- [ ] Resend account created (https://resend.com)
- [ ] API key generated and saved

### Railway Account
- [ ] Account created at https://railway.app
- [ ] Verified email

## Deployment Steps

### Step 1: Prepare Local Repository
```bash
cd c:\KodeKlub\kodeklubb_prosjekt
git add .
git commit -m "Configure Railway deployment"
git push origin main
```

### Step 2: Create Railway Project
1. Go to https://railway.app/dashboard
2. Click "New Project"
3. Connect your GitHub repository

### Step 3: Add Services (in order)

#### 1. PostgreSQL Database
- [ ] Add PostgreSQL service
- [ ] Railway auto-generates DATABASE_URL
- [ ] Save connection details

#### 2. Backend API (.NET)
- [ ] Add service from GitHub repo
- [ ] Set root directory: `backend/src`
- [ ] Set Dockerfile: `Dockerfile`
- [ ] Add environment variables (see table below)
- [ ] Generate domain name
- [ ] Wait for deployment

#### 3. Frontend UI (Vue.js)
- [ ] Add service from GitHub repo
- [ ] Set root directory: `frontend`
- [ ] Set Dockerfile: `Dockerfile`
- [ ] Add environment variables (see table below)
- [ ] Generate domain name
- [ ] Wait for deployment

### Step 4: Update Discord OAuth

1. Go to Discord Developer Portal
2. Select your application
3. Add OAuth2 Redirect URI:
   - Format: `https://<backend-railway-domain>/auth/discord/callback`
   - Example: `https://kodeklubb-api-prod.up.railway.app/auth/discord/callback`

### Step 5: Update Environment Variables

#### Backend Variables
```
ASPNETCORE_ENVIRONMENT = Production
ConnectionStrings__DefaultConnection = ${{Postgres.DATABASE_URL}}
Discord__ClientId = <your_discord_client_id>
Discord__ClientSecret = <your_discord_client_secret>
Discord__RedirectUri = https://<backend-domain>/auth/discord/callback
Discord__FrontendRedirectUri = https://<frontend-domain>
AllowedOrigins = https://<frontend-domain>,https://<backend-domain>
RESEND_API_KEY = <your_resend_api_key> (if using)
RESEND_FROM_EMAIL = updates@updates.getacademy.no (if using)
```

#### Frontend Variables
```
VITE_BASE_API = https://<backend-domain>
VITE_LOGIN_API = /auth/discord/login
```

### Step 6: Verify Deployment

- [ ] Both services show "Running" status
- [ ] Backend logs show "Application started"
- [ ] Frontend is accessible at its domain
- [ ] Database is connected (check backend logs)

### Step 7: Test Application

- [ ] Visit frontend URL in browser
- [ ] Click "Discord Login" button
- [ ] Redirected to Discord OAuth consent screen
- [ ] After approval, redirected back to app
- [ ] Logged in successfully

## Troubleshooting Checklist

If something goes wrong, check:

### Service Won't Deploy
- [ ] Dockerfile paths are correct
- [ ] No syntax errors in code
- [ ] All dependencies are listed
- [ ] Check deployment logs for specific errors

### CORS Errors in Console
- [ ] `AllowedOrigins` includes frontend domain
- [ ] No typos in environment variables
- [ ] Frontend is using correct API URL

### Discord Login Redirects to Error
- [ ] Discord redirect URI matches exactly in Discord Portal
- [ ] Discord credentials are correct
- [ ] Backend domain is accessible

### Database Connection Fails
- [ ] PostgreSQL service is running
- [ ] `ConnectionStrings__DefaultConnection` is set
- [ ] Using Railway's DATABASE_URL variable

### Frontend Shows Blank Page
- [ ] `VITE_BASE_API` points to correct backend URL
- [ ] Build completed successfully (check logs)
- [ ] dist/ folder exists with built files

## Post-Deployment

- [ ] Monitor application logs regularly
- [ ] Set up error alerts
- [ ] Test all main user flows
- [ ] Document any custom configurations
- [ ] Set up database backups
- [ ] Configure auto-deployments from specific branches

## Useful Links

- Railway Documentation: https://docs.railway.app/
- Railway CLI: https://docs.railway.app/develop/cli
- Discord Developers: https://discord.com/developers/applications
- Resend Documentation: https://resend.com/docs

## Support

For issues:
1. Check Railway logs in dashboard
2. Check Dockerfile for errors
3. Verify all environment variables
4. Check Discord Portal configuration
5. Compare with guide documentation

---

**Status**: Ready for deployment ✅
**Last Updated**: March 31, 2026
