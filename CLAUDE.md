# SpotMe Project Guidelines

## Development Environment
- Start all containers: `docker compose up -d`
- Stop all containers: `docker compose down`
- View logs: `docker compose logs -f`

## Build & Test Commands
- Build solution: `docker exec -it spotme-dotnet-sdk dotnet build /app/Source/SpotMe.sln`
- Run web app: `docker exec -it spotme-dotnet-sdk dotnet run --project /app/Source/SpotMe.Web/SpotMe.Web.csproj`
- Watch mode: `docker exec -it spotme-dotnet-sdk dotnet watch --project /app/Source/SpotMe.Web/SpotMe.Web.csproj run`
- Run tests: `docker exec -it spotme-dotnet-sdk dotnet test /app/Source/SpotMe.sln`
- Run single test: `docker exec -it spotme-dotnet-sdk dotnet test /app/Source/SpotMe.Tests/SpotMe.Tests.csproj --filter "FullyQualifiedName=SpotMe.Tests.NamespaceTests.TestName"`

## Code Style Guidelines
- Use C# 12 features when appropriate
- Follow Microsoft's .NET coding conventions
- Public members should have XML documentation comments
- Use dependency injection for services
- Prefer async/await for asynchronous operations
- Use nullable reference types
- Group using statements by: System, Microsoft, 3rd party libraries, project libraries
- Use manual mapping between models and DTOs (no AutoMapper)
- Place mapping logic in extension methods or dedicated mapper classes