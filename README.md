# SpotMe

A Spotify statistics application with a .NET 8 backend API and SvelteKit frontend, using PostgreSQL for data storage.

## Project Structure

- **SpotMe.Model**: Domain model classes for persistence
- **SpotMe.Web**: .NET 8 backend API (ASP.NET Core)
- **spotme-frontend**: SvelteKit frontend application

## Prerequisites

- **Docker** and **Docker Compose**
- **Git**

## Quick Start

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd spot-me
   ```

2. Start all services with Docker Compose:
   ```bash
   docker-compose up --build
   ```

3. Access the application:
   - **Frontend**: http://localhost:5173
   - **Backend API**: http://localhost:5000
   - **Database**: localhost:5432

That's it! All services will start with hot reload enabled.

## Service URLs

When running locally, the following services are available:

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:5173 | SvelteKit web application (hot reload enabled) |
| **Backend API** | http://localhost:5000 | .NET 8 REST API (hot reload via `dotnet watch`) |
| **API Endpoints** | http://localhost:5000/api | All API routes are under `/api` |
| **PostgreSQL** | localhost:5432 | Database connection |
| **SMTP4dev** | http://localhost:3000 | Development email server (for testing emails) |
| **File Browser** | http://localhost:8081 | Web UI to browse uploaded user files |

## Development Features

### Hot Reload

Both frontend and backend support hot reload:

- **Frontend**: Vite automatically reloads on `.svelte`, `.ts`, `.js`, and `.css` changes
- **Backend**: `dotnet watch` automatically rebuilds and restarts on `.cs` file changes
- **Database**: PostgreSQL data persists in Docker volumes

### Database Connection

The database is automatically configured in Docker. Connection details:
- **Host**: `postgres` (within Docker network) or `localhost` (from host)
- **Port**: `5432`
- **Username**: `spotme`
- **Password**: `spotme_password`
- **Database**: `spotme_db`

## Common Commands

### Start Services
```bash
# Start all services
docker-compose up

# Start in background (detached mode)
docker-compose up -d

# Rebuild and start
docker-compose up --build
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f frontend
docker-compose logs -f app
docker-compose logs -f postgres
```

### Stop Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (⚠️ deletes database data)
docker-compose down -v
```

### Database Commands
```bash
# Connect to PostgreSQL
docker exec -it spotme-postgres psql -U spotme -d spotme_db

# Run migrations manually (usually auto-applied on startup)
docker exec -it spotme-app-dev dotnet ef database update --project SpotMe.Web
```

## Troubleshooting

### Port Already in Use
If a port is already in use, you can:
- Stop the conflicting service
- Or modify the port mapping in `docker-compose.yml`

### Frontend Not Loading
- Check if the frontend container is running: `docker-compose ps`
- View frontend logs: `docker-compose logs frontend`
- Ensure port 5173 is not in use

### Backend Not Responding
- Check backend logs: `docker-compose logs app`
- Verify database is healthy: `docker-compose ps postgres`
- Check if port 5000 is available

### Database Connection Issues
- Ensure PostgreSQL container is running: `docker-compose ps postgres`
- Check database health: `docker exec spotme-postgres pg_isready -U spotme`
- View database logs: `docker-compose logs postgres`

### Hot Reload Not Working
- For frontend: Ensure files are saved and Vite detects changes
- For backend: Check that `dotnet watch` is running (should see "watch" in logs)
- Try restarting the specific service: `docker-compose restart frontend` or `docker-compose restart app`

## Project Architecture

- **Backend**: RESTful API built with ASP.NET Core Minimal APIs
- **Frontend**: SvelteKit with TypeScript
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT-based authentication
- **File Storage**: User-uploaded Spotify data files stored in Docker volumes

## Production Deployment (Azure)

### Overview

For production deployment on Azure App Service, the frontend is built as static files and served from the backend's `wwwroot` directory. Both frontend and backend run on the same domain, so CORS is not required.

### Build Process

1. **Build Frontend**:
   ```bash
   cd spotme-frontend
   npm install
   npm run build
   ```

2. **Copy Frontend to Backend**:
   ```bash
   # Copy build output to wwwroot
   cp -r spotme-frontend/build/* Source/SpotMe.Web/wwwroot/
   ```

3. **Build Backend**:
   ```bash
   dotnet publish Source/SpotMe.Web/SpotMe.Web.csproj -c Release
   ```

4. **Deploy to Azure**: Deploy the published output to Azure App Service

### Required Environment Variables (Azure App Settings)

Configure these in Azure Portal under **Configuration > Application settings**:

#### Required Settings

- **`Jwt__SecretKey`** (required)
  - Minimum 32 characters
  - Used for JWT token signing
  - Example: `YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!`

- **`ConnectionStrings__Database`** (required)
  - PostgreSQL connection string
  - Format: `Host=<host>;Database=<db>;Username=<user>;Password=<password>;Port=5432`
  - For Azure Database for PostgreSQL: `Host=<server>.postgres.database.azure.com;Database=<db>;Username=<user>;Password=<password>;Port=5432;Ssl Mode=Require`

- **`Spotify__ClientId`** (required for Spotify integration)
  - Spotify application client ID

- **`Spotify__ClientSecret`** (required for Spotify integration)
  - Spotify application client secret

- **`Spotify__RedirectUri`** (required for Spotify integration)
  - Must match the redirect URI configured in your Spotify app
  - Format: `https://yourapp.azurewebsites.net/api/spotify/callback`

#### Optional Settings

- **`Jwt__Issuer`** (optional, defaults to "SpotMe")
  - JWT token issuer

- **`Jwt__Audience`** (optional, defaults to "SpotMeUsers")
  - JWT token audience

- **`Jwt__ExpirationMinutes`** (optional, defaults to 60)
  - JWT token expiration time in minutes

### GitHub Actions Deployment

For automated deployment, create a GitHub Actions workflow (`.github/workflows/deploy.yml`) that:

1. Builds the frontend: `cd spotme-frontend && npm install && npm run build`
2. Copies frontend build output to `Source/SpotMe.Web/wwwroot/`
3. Builds the backend: `dotnet publish`
4. Deploys to Azure App Service

### Docker Production Deployment

If using Docker on Azure Container Instances or Azure Container Apps:

1. Use `docker-compose.prod.yml` for production
2. Set all environment variables in the compose file or via `.env` file
3. The Dockerfile automatically builds the frontend and includes it in the image

### Notes

- CORS is disabled in production since frontend and backend are on the same domain
- The backend serves `index.html` as a fallback for SPA routing
- All sensitive values should be stored in Azure App Settings, not in `appsettings.Production.json`
- Database migrations are automatically applied on application startup

## Additional Documentation

- **HOW_TO_RUN.md**: Detailed setup instructions (legacy, now using Docker)
- **DOCKER_SETUP.md**: Docker-specific configuration and data management
- **ARCHITECTURE.md**: System architecture and design decisions
