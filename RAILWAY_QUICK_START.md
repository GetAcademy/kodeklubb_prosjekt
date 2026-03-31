# Railway Quick Start Guide

## 1. Prerequisites
- [ ] GitHub repository with your code pushed
- [ ] Railway account (https://railway.app)
- [ ] Discord OAuth app credentials
- [ ] Resend API key (optional, for email)

## 2. Push Your Code to GitHub
```bash
git add .
git commit -m "Add Railway deployment configuration"
git push origin main
```

## 3. Create Railway Project

### Step A: Create PostgreSQL Database
1. Go to https://railway.app/dashboard
2. Click "New" → "New Service" → "PostgreSQL"
3. Accept the defaults
4. Copy the Database URL

### Step B: Deploy Backend API
1. Click "New" → "GitHub Repo"
2. Select your repository
3. In the service settings:
   - **Name**: `kodeklubb-api`
   - **Root Directory**: `backend/src`
   - **Dockerfile**: `Dockerfile`
   
4. Add Environment Variables:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
   Discord__ClientId=<YOUR_DISCORD_CLIENT_ID>
   Discord__ClientSecret=<YOUR_DISCORD_CLIENT_SECRET>
   Discord__RedirectUri=${{Railway.DOMAIN}}/auth/discord/callback
   Discord__FrontendRedirectUri=<YOUR_FRONTEND_URL>
   AllowedOrigins=${{Railway.DOMAIN}},<YOUR_FRONTEND_URL>
   RESEND_API_KEY=<YOUR_RESEND_API_KEY>
   ```

5. Click "Deploy"

### Step C: Deploy Frontend UI
1. Click "New" → "GitHub Repo"
2. Select the same repository
3. In the service settings:
   - **Name**: `kodeklubb-frontend`
   - **Root Directory**: `frontend`
   - **Dockerfile**: `Dockerfile`

4. Add Environment Variables:
   ```
   VITE_BASE_API=${{kodeklubb-api.RAILWAY_DOMAIN}}
   VITE_LOGIN_API=/auth/discord/login
   ```

5. Click "Deploy"

## 4. Configure Discord OAuth

1. Go to Discord Developer Portal (https://discord.com/developers/applications)
2. Select your application
3. Go to "OAuth2" → "Redirects"
4. Add redirect URI: 
   ```
   https://<backend-domain>/auth/discord/callback
   ```

## 5. Generate Public Domains

1. In Railway dashboard, go to each service
2. Click "Generate Domain" under the service name
3. Copy the domains and update:
   - Backend: Use as `VITE_BASE_API` in frontend
   - Frontend: Use as `Discord__FrontendRedirectUri` in backend

## 6. Test Deployment

1. Visit your frontend URL
2. Click "Discord Login"
3. You should be redirected to Discord OAuth
4. After authorization, you should be logged in

## Troubleshooting

### Deployment Failed
- Check the deployment logs in Railway dashboard
- Verify Dockerfile paths are correct
- Ensure all required files are in the repository

### CORS Errors
- Update `AllowedOrigins` in backend environment variables
- Include both frontend and Railway domain URLs

### Discord Login Not Working
- Verify Discord redirect URI in Discord Developer Portal
- Check Discord credentials in environment variables
- View logs for specific error messages

### Database Connection Error
- Ensure `ConnectionStrings__DefaultConnection` is set correctly
- Check PostgreSQL service is running
- Verify database credentials

## Useful Railway CLI Commands

```bash
# Install Railway CLI
npm install -g @railway/cli

# Login
railway login

# View all projects
railway list

# Deploy from local
railway deploy

# View logs
railway logs --tail

# Set environment variable
railway variables set KEY=VALUE
```

## Environment Variables Map

| Variable | Backend | Frontend | Example |
|----------|---------|----------|---------|
| API URL | - | ✅ VITE_BASE_API | https://api.railway.app |
| Login Endpoint | - | ✅ VITE_LOGIN_API | /auth/discord/login |
| Environment | ✅ ASPNETCORE_ENVIRONMENT | - | Production |
| Database Connection | ✅ ConnectionStrings__DefaultConnection | - | postgresql://... |
| Discord Client ID | ✅ Discord__ClientId | - | 1234567890 |
| Discord Secret | ✅ Discord__ClientSecret | - | secret |
| Discord Redirect | ✅ Discord__RedirectUri | - | https://api.railway.app/callback |
| Frontend URL | ✅ Discord__FrontendRedirectUri | - | https://frontend.railway.app |
| CORS Origins | ✅ AllowedOrigins | - | https://frontend.railway.app |

## Next Steps

1. [ ] Set up automated deployments from specific branch
2. [ ] Configure monitoring and alerts
3. [ ] Set up custom domain (if needed)
4. [ ] Configure email templates (if using Resend)
5. [ ] Set up database backups
6. [ ] Monitor application performance

For more info: https://docs.railway.app/
