# Railway Deployment Guide

## Prerequisites
1. Railway account (create at https://railway.app)
2. GitHub repository (push your code)
3. Discord OAuth credentials
4. Resend API key (for emails)

## Step 1: Prepare Your Project

### 1.1 Update Environment Variables
Create production environment files:

**Backend (.env for production):**
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=postgresql://<RAILWAY_DB_USER>:<RAILWAY_DB_PASSWORD>@<RAILWAY_DB_HOST>:5432/<RAILWAY_DB_NAME>
Discord__ClientId=your_discord_client_id
Discord__ClientSecret=your_discord_client_secret
RESEND_API_KEY=your_resend_api_key
```

**Frontend (.env.production):**
```
VITE_BASE_API=https://your-api-domain.com
VITE_LOGIN_API=/auth/discord/login
```

## Step 2: Deploy on Railway

### 2.1 Connect GitHub Repository
1. Go to https://railway.app
2. Click "New Project" → "Deploy from GitHub repo"
3. Select your repository

### 2.2 Create Services

#### Database Service
1. Click "Add Service" → "PostgreSQL"
2. Railway will automatically create a PostgreSQL instance
3. Note the connection details

#### Backend Service (.NET API)
1. Click "Add Service" → "GitHub Repo"
2. Select the repository
3. In service settings:
   - **Name**: kodeklubb-api
   - **Root Directory**: `backend/src`
   - **Dockerfile**: `Dockerfile`
   - **Port**: 8080
   - **Startup Command**: (leave empty - uses Dockerfile ENTRYPOINT)

4. Add Environment Variables:
   - `ASPNETCORE_ENVIRONMENT`: Production
   - `ConnectionStrings__DefaultConnection`: ${{Postgres.DATABASE_URL}}
   - `Discord__ClientId`: (your Discord client ID)
   - `Discord__ClientSecret`: (your Discord client secret)
   - `RESEND_API_KEY`: (your Resend API key)

#### Frontend Service (Vue.js)
1. Click "Add Service" → "GitHub Repo"
2. Select the repository
3. In service settings:
   - **Name**: kodeklubb-frontend
   - **Root Directory**: `frontend`
   - **Dockerfile**: `Dockerfile`
   - **Port**: 3000
   - **Startup Command**: (leave empty - uses Dockerfile CMD)

4. Add Environment Variables:
   - `VITE_BASE_API`: https://<backend-service-url>
   - `VITE_LOGIN_API`: /auth/discord/login

### 2.3 Configure Networking
1. In Backend Service → "Generate Domain" to get a public URL
2. Copy the backend URL
3. In Frontend Service → Set environment variable:
   - `VITE_BASE_API`: <backend_url>

### 2.4 Deploy
1. Each service will deploy automatically from the github branch
2. Monitor the deployment logs
3. Once complete, you'll get public URLs for both services

## Step 3: Verify Deployment

### Test Backend API
```
curl https://<backend-url>/login
```

### Test Frontend
Visit: `https://<frontend-url>`

### Test Discord Login
1. Click Discord button on frontend
2. You should be redirected to Discord OAuth
3. After login, you should be redirected back to your app

## Step 4: Production Configuration

### Database Migrations
Railway will run migrations automatically if configured in your `Program.cs`:
- Migrations run on startup via `DatabaseMigrator.MigrateAsync()`

### Monitor Logs
1. Go to each service in Railway
2. View real-time logs in the Dashboard
3. Check for any errors during runtime

## Troubleshooting

### Service Won't Start
- Check the deployment logs in Railway dashboard
- Verify all environment variables are set
- Ensure Dockerfile is correct

### Database Connection Issues
- Verify PostgreSQL service is running
- Check connection string format
- Ensure DATABASE_URL or ConnectionStrings__DefaultConnection is set

### Frontend Not Loading API
- Verify VITE_BASE_API points to correct backend URL
- Check CORS configuration in backend
- Verify Discord redirect URI in Discord Developer Portal

### Discord Login Fails
- Check Discord OAuth credentials are correct
- Update Discord Developer Portal with new redirect URI (frontend URL + /callback)
- Verify FrontendRedirectUri matches your deployed frontend URL

## Environment Variables Reference

### Backend (.NET)
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<database_url>
Discord__ClientId=<discord_client_id>
Discord__ClientSecret=<discord_client_secret>
Discord__RedirectUri=<backend_url>/auth/discord/callback
Discord__FrontendRedirectUri=<frontend_url>
RESEND_API_KEY=<resend_api_key>
```

### Frontend (Vue.js)
```
VITE_BASE_API=<backend_url>
VITE_LOGIN_API=/auth/discord/login
```

## Useful Railway Commands

### Using Railway CLI (Optional)
```bash
# Install Railway CLI
npm i -g @railway/cli

# Login
railway login

# Link to Railway project
railway link

# View environment variables
railway variables

# Set environment variable
railway variables set KEY=VALUE

# View logs
railway logs
```

## Next Steps
1. Configure custom domain (if needed)
2. Set up monitoring and alerts
3. Enable automatic deployments from specific branches
4. Configure backup strategy for database
