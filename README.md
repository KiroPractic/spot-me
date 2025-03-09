# SpotMe

A .NET 8 application with Blazor frontend and Entity Framework Core backend using PostgreSQL.

## Project Structure

- **SpotMe.Model**: Contains domain model classes for persistence
- **SpotMe.Web**: Blazor Server application (web frontend and backend)

## Development Setup

### Prerequisites

- Docker and Docker Compose
- Git

### Getting Started

1. Clone the repository
2. Make sure Docker is installed
3. Start the development environment:
   ```
   docker compose up -d
   ```

### Development Tools

- **PostgreSQL**: Running on port 5432
  - Username: spotme
  - Password: spotme_password
  - Database: spotme_db
- **SMTP Server (smtp4dev)**: Web interface available at http://localhost:3000
- **Blazor Application**: The web application will be available at http://localhost:5000 when running

## Common Commands

Build the solution:
```
docker exec -it spotme-dotnet-sdk dotnet build /app/SpotMe.sln
```

Run the web application:
```
docker exec -it spotme-dotnet-sdk dotnet run --project /app/SpotMe.Web/SpotMe.Web.csproj
```

Run in watch mode (auto-reload on code changes):
```
docker exec -it spotme-dotnet-sdk dotnet watch --project /app/SpotMe.Web/SpotMe.Web.csproj run
```

Stop all containers:
```
docker compose down
```

## Future Plans

- Android application using .NET MAUI

## Additional Information

See CLAUDE.md for detailed coding guidelines and additional commands.