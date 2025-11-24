# Frontend build stage
FROM node:20 AS frontend-build
WORKDIR /app/frontend

# Copy frontend package files
COPY spotatrend-frontend/package*.json ./

# Install dependencies
RUN npm ci

# Copy frontend source
COPY spotatrend-frontend/ .

# Build frontend
RUN npm run build

# Backend build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY Source/SpotATrend.Model/SpotATrend.Model.csproj SpotATrend.Model/
COPY Source/SpotATrend.Web/SpotATrend.Web.csproj SpotATrend.Web/

# Restore dependencies
RUN dotnet restore SpotATrend.Web/SpotATrend.Web.csproj

# Copy source code
COPY Source/ .

# Copy frontend build output to wwwroot
COPY --from=frontend-build /app/frontend/build ./SpotATrend.Web/wwwroot/

# Build the application
RUN dotnet build SpotATrend.Web/SpotATrend.Web.csproj -c Release -o /app/build

# Publish the application
RUN dotnet publish SpotATrend.Web/SpotATrend.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Copy SpotifyStats directory
COPY Source/SpotATrend.Web/SpotifyStats ./SpotifyStats/

# Create UserData directory
RUN mkdir -p ./UserData

# Create non-root user
RUN groupadd -r spotatrend && useradd -r -g spotatrend spotatrend
RUN chown -R spotatrend:spotatrend /app
USER spotatrend

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "SpotATrend.Web.dll"] 