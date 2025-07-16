# SpotMe Docker Setup Guide

## 🐳 Complete Docker Setup with Persistent Data

This guide explains how to set up SpotMe with Docker, including persistent storage for both database and user files.

## 📁 Data Persistence Overview

### Database (PostgreSQL)
- **Volume**: `postgres_data`
- **Purpose**: Stores user accounts, preferences, and relational data
- **Persistence**: Data survives container restarts and updates

### Global Spotify Stats
- **Volume**: `spotify_data` 
- **Path**: `/src/SpotMe.Web/SpotifyStats` (dev) or `/app/SpotifyStats` (prod)
- **Purpose**: Stores the default/global Spotify streaming history JSON files
- **Persistence**: Files survive container restarts

### User-Specific Data
- **Volume**: `user_data`
- **Path**: `/src/SpotMe.Web/UserData` (dev) or `/app/UserData` (prod)
- **Structure**: `/UserData/{userId}/SpotifyStats/`
- **Purpose**: Each user can upload their own Spotify data files
- **Persistence**: User files survive container restarts

## 🚀 Getting Started

### Development Environment

1. **Start the development environment:**
   ```bash
   docker-compose up --build
   ```

2. **Access the application:**
   - SpotMe app: http://localhost:5000
   - SMTP4dev: http://localhost:3000
   - PostgreSQL: localhost:5432

3. **Upload your Spotify data:**
   - Navigate to http://localhost:5000/userdata
   - Upload your JSON files downloaded from Spotify
   - View your personalized stats at http://localhost:5000/stats

### Production Deployment

1. **Create environment file:**
   ```bash
   cp .env.example .env
   # Edit .env with your actual secrets
   ```

2. **Deploy to production:**
   ```bash
   docker-compose -f docker-compose.prod.yml up -d --build
   ```

3. **Access the application:**
   - SpotMe app: http://localhost (port 80)

## 📊 Data Flow

```
User Upload → UserDataService → Persistent Volume → StatsService → Display
     ↓
JSON Files stored in: /UserData/{userId}/SpotifyStats/
     ↓
Auto-cached and processed by StatsService
     ↓
Displayed in Stats page with user-specific data
```

## 🔧 Managing Data

### Copy Existing Data to Containers

**Copy your existing JSON files to the persistent volume:**
```bash
# For development
docker cp ./path/to/your/spotify/files/. spotme-app-dev:/src/SpotMe.Web/UserData/demo-user/SpotifyStats/

# For production  
docker cp ./path/to/your/spotify/files/. spotme-app-prod:/app/UserData/demo-user/SpotifyStats/
```

### Backup Your Data

**Backup user files:**
```bash
# Development
docker cp spotme-app-dev:/src/SpotMe.Web/UserData ./backup-user-data

# Production
docker cp spotme-app-prod:/app/UserData ./backup-user-data
```

**Backup database:**
```bash
docker exec spotme-postgres pg_dump -U spotme spotme_db > backup.sql
```

### Restore Data

**Restore user files:**
```bash
# Development
docker cp ./backup-user-data/. spotme-app-dev:/src/SpotMe.Web/UserData/

# Production
docker cp ./backup-user-data/. spotme-app-prod:/app/UserData/
```

**Restore database:**
```bash
docker exec -i spotme-postgres psql -U spotme spotme_db < backup.sql
```

## 🗂️ Directory Structure

```
SpotMe/
├── UserData/                    # User-specific data (persistent volume)
│   └── {userId}/
│       └── SpotifyStats/
│           ├── Streaming_History_Audio_2023_1.json
│           ├── Streaming_History_Audio_2024_2.json
│           └── ...
├── SpotifyStats/               # Global/default data (persistent volume)
│   ├── Streaming_History_Audio_2021-2023_0.json
│   └── ...
└── postgres_data/             # Database files (persistent volume)
```

## 🔒 Data Security

- All data stored in named Docker volumes
- Non-root user runs the application
- User data isolated by user ID
- JSON validation on upload
- File size limits enforced

## 🛠️ Database Migrations

The application automatically applies database migrations on startup:

```csharp
// Auto-applied during container startup
await context.Database.MigrateAsync();
```

### Manual Migration Commands

If you need to create new migrations:

```bash
# Create a new migration
docker exec -it spotme-app-dev dotnet ef migrations add MigrationName --project SpotMe.Web

# Apply migrations manually
docker exec -it spotme-app-dev dotnet ef database update --project SpotMe.Web

# Remove last migration
docker exec -it spotme-app-dev dotnet ef migrations remove --project SpotMe.Web
```

## 📱 User Experience

### For Users Without Data
- Access basic app functionality
- See global stats (if available)
- Upload their own data via `/userdata` page

### For Users With Data
- Upload JSON files through the web interface
- View personalized statistics
- Manage their uploaded files
- Data persists across deployments

## 🐛 Troubleshooting

### Container Won't Start
```bash
# Check logs
docker-compose logs app
docker-compose logs postgres

# Restart individual services
docker-compose restart app
docker-compose restart postgres
```

### Database Connection Issues
```bash
# Check PostgreSQL status
docker exec spotme-postgres pg_isready -U spotme

# Reset database (⚠️ DESTRUCTIVE)
docker-compose down
docker volume rm spotme_postgres_data
docker-compose up --build
```

### User Data Not Loading
```bash
# Check if directory exists
docker exec spotme-app-dev ls -la /src/SpotMe.Web/UserData/demo-user/

# Check file permissions
docker exec spotme-app-dev ls -la /src/SpotMe.Web/UserData/demo-user/SpotifyStats/

# Clear cache and restart
docker exec spotme-app-dev rm -rf /src/SpotMe.Web/bin /src/SpotMe.Web/obj
docker-compose restart app
```

### Volume Issues
```bash
# List all volumes
docker volume ls

# Inspect volume details
docker volume inspect spotme_user_data
docker volume inspect spotme_postgres_data

# Remove all data (⚠️ DESTRUCTIVE)
docker-compose down -v
docker-compose up --build
```

## 🔄 Updates and Maintenance

### Update Application
```bash
# Pull latest code
git pull

# Rebuild and restart (data persists)
docker-compose down
docker-compose up --build

# For production
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up --build -d
```

### Clean Up
```bash
# Remove unused images
docker image prune

# Remove unused volumes (⚠️ Check first!)
docker volume ls
docker volume prune

# Complete cleanup (⚠️ DESTRUCTIVE - removes all data)
docker-compose down -v
docker system prune -a
```

## 🎯 Next Steps

1. **Set up authentication** - Replace hardcoded `demo-user` with real user management
2. **Add user registration** - Create user accounts in the database
3. **Implement file validation** - Enhanced security for uploaded files
4. **Add file sharing** - Allow users to share stats publicly
5. **Backup automation** - Scheduled backups of user data and database

## 📞 Support

If you encounter issues:
1. Check the logs: `docker-compose logs -f app`
2. Verify volumes exist: `docker volume ls`
3. Test database connection: `docker exec spotme-postgres pg_isready -U spotme`
4. Check file permissions in containers
5. Restart services: `docker-compose restart` 