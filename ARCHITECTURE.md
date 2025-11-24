# SpotATrend Architecture

## Overview

SpotATrend is now a **separated frontend/backend application**:

- **Backend**: ASP.NET Core API (FastEndpoints)
- **Frontend**: SvelteKit application

## Directory Structure

```
spot-a-trend/
├── Source/
│   └── SpotATrend.Web/          # Backend API
│       ├── wwwroot/         # Static assets only (favicon, images)
│       ├── Endpoints/        # API endpoints
│       └── Program.cs       # Backend configuration
│
└── spotatrend-frontend/         # Frontend SvelteKit app
    ├── src/
    │   ├── routes/          # SvelteKit routes (pages)
    │   ├── lib/             # Shared components and utilities
    │   └── app.css          # Global styles
    └── package.json
```

## Why This Structure?

### Before (HTMX)
- All HTML files were in `wwwroot/`
- Backend served static HTML files directly
- HTMX handled dynamic updates via AJAX
- Multiple HTML files (index.html, login.html, etc.)

### After (SvelteKit)
- Frontend is a separate SPA (Single Page Application)
- SvelteKit handles routing client-side
- Only one entry point (SvelteKit's router)
- Better developer experience with TypeScript, hot reload, etc.
- Can be deployed separately or together

## Development

**Backend** (ASP.NET Core):
- Runs on port 5000 (or configured port)
- Serves API at `/api/*`
- CORS enabled for frontend communication

**Frontend** (SvelteKit):
- Runs on port 5173 (Vite default)
- Makes API calls to backend
- Handles all UI and routing

## Production Deployment Options

### Option 1: Separate Deployment
- Deploy backend to one server/domain
- Deploy frontend to another (CDN, static hosting, etc.)
- Configure CORS appropriately

### Option 2: Combined Deployment
- Build SvelteKit: `npm run build`
- Copy build output to `wwwroot/`
- Backend serves both API and static files
- Update `Program.cs` to serve SvelteKit's `index.html` as fallback

### Option 3: Reverse Proxy
- Use nginx/traefik to route:
  - `/api/*` → Backend
  - `/*` → Frontend
- Both can run on same domain

## Migration Notes

The old HTMX files have been removed:
- ❌ `wwwroot/*.html` (migrated to SvelteKit routes)
- ❌ `wwwroot/js/*.js` (migrated to SvelteKit components)
- ❌ `wwwroot/app.css` (moved to `spotatrend-frontend/src/app.css`)
- ✅ `wwwroot/favicon.png` (kept for static assets)
- ✅ `wwwroot/images/` (kept for static assets)

