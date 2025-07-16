# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY Source/SpotMe.Model/SpotMe.Model.csproj SpotMe.Model/
COPY Source/SpotMe.Web/SpotMe.Web.csproj SpotMe.Web/

# Restore dependencies
RUN dotnet restore SpotMe.Web/SpotMe.Web.csproj

# Copy source code
COPY Source/ .

# Build the application
RUN dotnet build SpotMe.Web/SpotMe.Web.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish SpotMe.Web/SpotMe.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Copy SpotifyStats directory
COPY Source/SpotMe.Web/SpotifyStats ./SpotifyStats/

# Create UserData directory
RUN mkdir -p ./UserData

# Create non-root user
RUN groupadd -r spotme && useradd -r -g spotme spotme
RUN chown -R spotme:spotme /app
USER spotme

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "SpotMe.Web.dll"] 