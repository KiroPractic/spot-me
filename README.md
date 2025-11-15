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

## Additional Documentation

- **HOW_TO_RUN.md**: Detailed setup instructions (legacy, now using Docker)
- **DOCKER_SETUP.md**: Docker-specific configuration and data management
- **ARCHITECTURE.md**: System architecture and design decisions
