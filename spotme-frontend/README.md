# SpotMe Frontend (SvelteKit)

This is the frontend application for SpotMe, built with SvelteKit.

## Architecture

The frontend and backend are now **separate applications**:

- **Backend (ASP.NET Core)**: Runs on `http://localhost:5000` (or configured port)
  - Serves only the API endpoints under `/api`
  - No longer serves static HTML files (those were removed from `wwwroot`)

- **Frontend (SvelteKit)**: Runs on `http://localhost:5173` (default Vite port)
  - Handles all UI and routing
  - Makes API calls to the backend

## Setup

1. Install dependencies:
```bash
npm install
```

2. Create a `.env` file with your API URL:
```
VITE_API_URL=http://localhost:5000/api
```

3. Run the development server:
```bash
npm run dev
```

The frontend will be available at `http://localhost:5173` and will communicate with the backend API.

## Production Build

To build for production:

```bash
npm run build
```

The built files will be in the `build` directory. You can:
- Deploy them separately (e.g., to a CDN or static hosting)
- Copy them to the backend's `wwwroot` folder if you want to serve them from the same domain
- Use a reverse proxy to serve both from the same domain

## Development Workflow

1. Start the backend: Run the ASP.NET Core application
2. Start the frontend: Run `npm run dev` in this directory
3. Access the app at `http://localhost:5173`

The frontend will automatically proxy API requests to the backend based on `VITE_API_URL`.
