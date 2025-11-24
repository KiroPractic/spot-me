# SpotATrend

A Spotify statistics application with a .NET 8 backend API and SvelteKit frontend, using PostgreSQL for data storage.

## Project Structure

- **SpotATrend.Model**: Domain model classes for persistence
- **SpotATrend.Web**: .NET 8 backend API (ASP.NET Core)
- **spotatrend-frontend**: SvelteKit frontend application

## Prerequisites

- **Docker** and **Docker Compose**
- **Git**

## Quick Start

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd spot-a-trend
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
- **Username**: `spotatrend`
- **Password**: `spotatrend_password`
- **Database**: `spotatrend_db`

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
docker exec -it spotatrend-postgres psql -U spotatrend -d spotatrend_db

# Run migrations manually (usually auto-applied on startup)
docker exec -it spotatrend-app-dev dotnet ef database update --project SpotATrend.Web
```






### GitHub Actions Deployment

For automated deployment, create a GitHub Actions workflow (`.github/workflows/deploy.yml`) that:

1. Builds the frontend: `cd spotatrend-frontend && npm install && npm run build`
2. Copies frontend build output to `Source/SpotATrend.Web/wwwroot/`
3. Builds the backend: `dotnet publish`
4. Deploys to Azure App Service


### Notes

- CORS is disabled in production since frontend and backend are on the same domain
- The backend serves `index.html` as a fallback for SPA routing
- All sensitive values should be stored in Azure App Settings, not in `appsettings.Production.json`
- Database migrations are automatically applied on application startup


## Additional Documentation

- **HOW_TO_RUN.md**: Detailed setup instructions (legacy, now using Docker)
- **DOCKER_SETUP.md**: Docker-specific configuration and data management
- **ARCHITECTURE.md**: System architecture and design decisions
