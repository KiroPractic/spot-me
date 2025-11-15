# How to Run SpotMe

SpotMe consists of two separate applications that need to run simultaneously:

1. **Backend (ASP.NET Core)** - API server
2. **Frontend (SvelteKit)** - Web application

## Prerequisites

- **.NET 8 SDK** - For the backend
- **Node.js 18+** - For the frontend
- **PostgreSQL** - Database (configured in `appsettings.json`)

## Step 1: Setup Backend

1. Navigate to the backend directory:
```bash
cd Source/SpotMe.Web
```

2. Restore dependencies (if needed):
```bash
dotnet restore
```

3. Configure database connection in `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "Database": "your-postgresql-connection-string"
  }
}
```

4. Run the backend:
```bash
dotnet run
```

The backend will start on:
- **HTTP**: `http://localhost:5002`
- **HTTPS**: `https://localhost:7183` or `http://localhost:5277`

**Note**: The backend serves only the API at `/api/*` endpoints.

## Step 2: Setup Frontend

1. Navigate to the frontend directory:
```bash
cd spotme-frontend
```

2. Install dependencies (first time only):
```bash
npm install
```

3. Create a `.env` file in the `spotme-frontend` directory:
```env
VITE_API_URL=http://localhost:5002/api
```

**Note**: Adjust the port if your backend runs on a different port (check `launchSettings.json`).

4. Run the frontend:
```bash
npm run dev
```

The frontend will start on:
- **http://localhost:5173**

## Step 3: Access the Application

Open your browser and navigate to:
```
http://localhost:5173
```

The frontend will automatically communicate with the backend API.

## Running Both Applications

You need **two terminal windows**:

### Terminal 1 - Backend:
```bash
cd Source/SpotMe.Web
dotnet run
```

### Terminal 2 - Frontend:
```bash
cd spotme-frontend
npm run dev
```

## Troubleshooting

### Backend Issues

- **Port already in use**: Change the port in `launchSettings.json` or kill the process using that port
- **Database connection error**: Check your PostgreSQL connection string in `appsettings.Development.json`
- **CORS errors**: The backend has CORS enabled, but verify it's allowing requests from `http://localhost:5173`

### Frontend Issues

- **API connection errors**: 
  - Verify the backend is running
  - Check that `VITE_API_URL` in `.env` matches your backend URL
  - Ensure the backend port matches (default is `5002` for HTTP)
- **Module not found**: Run `npm install` again
- **Port 5173 in use**: Vite will automatically try the next available port

### Common Issues

1. **"Cannot connect to API"**
   - Make sure backend is running
   - Check `.env` file has correct `VITE_API_URL`
   - Verify CORS is enabled in backend

2. **"401 Unauthorized"**
   - This is normal if you're not logged in
   - Try registering a new account or logging in

3. **Database migrations**
   - The backend automatically applies migrations on startup
   - If you see migration errors, check your database connection

## Development Workflow

1. Start backend first (Terminal 1)
2. Start frontend (Terminal 2)
3. Make changes to either codebase
4. Frontend has hot-reload (changes appear automatically)
5. Backend requires restart for changes (stop with Ctrl+C and run `dotnet run` again)

## Production Build

### Build Frontend:
```bash
cd spotme-frontend
npm run build
```

### Run Backend in Production:
```bash
cd Source/SpotMe.Web
dotnet publish -c Release
dotnet run --no-build
```

