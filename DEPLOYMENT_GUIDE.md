# Plantopia - Complete Setup Guide

## 🚀 Quick Start

### Prerequisites
- Unity 2022.3 or later
- Docker Desktop (for backend)
- OR Python 3.10+ with pip (for local backend)

## Backend Setup

### Option 1: Docker (Recommended for WebGL)

1. **Navigate to backend folder**:
```cmd
cd backend
```

2. **Start the backend**:
```cmd
docker-compose up -d
```

3. **Check if running**:
```cmd
curl http://localhost:5000/health
```

Expected response:
```json
{"status":"healthy","timestamp":"...","version":"1.0.0"}
```

4. **View logs**:
```cmd
docker-compose logs -f
```

5. **Stop backend**:
```cmd
docker-compose down
```

### Option 2: Local Python (Development)

1. **Install dependencies**:
```cmd
cd backend
pip install -r requirements.txt
```

2. **Set API key** (edit `.env` file):
```
OPENTOPO_API_KEY=your_key_here
```

3. **Run Flask server**:
```cmd
python app.py
```

Server runs on `http://localhost:5000`

## Unity Configuration

### 1. Configure API Settings

Edit `Assets/StreamingAssets/Config/api_config.json`:

```json
{
  "backend": {
    "backendUrl": "http://localhost:5000",  // Change for production
    "useLocalAPI": true,  // true = use backend, false = direct API calls
    "fallbackToDirect": true
  },
  "openTopography": {
    "apiKey": "your_key_here"  // For fallback mode
  }
}
```

### 2. Unity Scene Setup

1. **Open Unity Project**
2. **Create new scene** or open existing
3. **Menu: Tools → Plantopia → Quick Setup**
4. **This creates**:
   - TerrainLoader GameObject with all components
   - Main Camera
   - Directional Light
   - UI Canvas (optional)

### 3. Test the System

1. **Select TerrainLoader** in Hierarchy
2. **Inspector → System Tester section**
3. **Right-click → Test All Systems**
4. **Check console for**:
   - ✓ Python installed (if using local processing)
   - ✓ Backend API reachable (if using backend)
   - ✓ API key configured
   - ✓ Paths configured

### 4. Generate Terrain

1. **Select TerrainLoader**
2. **Enter location name**: "Grand Canyon"
3. **Click "Load Terrain"**
4. **Watch progress** in console
5. **Terrain appears** in scene

## Deployment

### WebGL Build

**Important**: WebGL cannot run Python scripts. You MUST use the backend API!

1. **Set `useLocalAPI: true`** in `api_config.json`
2. **Deploy backend** to cloud server:
   - Heroku
   - AWS Elastic Beanstalk
   - Google Cloud Run
   - Azure App Service
   - DigitalOcean App Platform

3. **Update backendUrl** in `api_config.json`:
```json
"backendUrl": "https://your-api-server.com"
```

4. **Build WebGL**:
   - File → Build Settings
   - Platform: WebGL
   - Build and Run

### Desktop Build (Windows/Mac/Linux)

Desktop builds can use either:
- **Backend API** (recommended): Set `useLocalAPI: true`
- **Local Python** (requires Python installed): Set `useLocalAPI: false`

## Backend Deployment Examples

### Heroku

```bash
cd backend
heroku create plantopia-api
heroku config:set OPENTOPO_API_KEY=your_key
git init
git add .
git commit -m "Initial commit"
git push heroku main
```

Get URL: `https://plantopia-api.herokuapp.com`

### Docker Hub + Cloud

```bash
# Build and push
cd backend
docker build -t yourusername/plantopia-backend .
docker push yourusername/plantopia-backend

# Deploy on any cloud with Docker support
# Use environment variable for API key
```

### AWS Elastic Beanstalk

```bash
cd backend
eb init -p python-3.10 plantopia-backend
eb create plantopia-api-env
eb setenv OPENTOPO_API_KEY=your_key
eb open
```

## Testing

### Test Backend API

```bash
# Health check
curl http://localhost:5000/health

# Geocode
curl -X POST http://localhost:5000/api/geocode \
  -H "Content-Type: application/json" \
  -d '{"location":"Mount Everest"}'

# Download DEM
curl -X POST http://localhost:5000/api/download-dem \
  -H "Content-Type: application/json" \
  -d '{
    "latitude":27.9881,
    "longitude":86.925,
    "radius_km":5,
    "api_key":"your_key"
  }'

# Process DEM (returns PNG file)
curl -X POST http://localhost:5000/api/process-dem \
  -H "Content-Type: application/json" \
  -d '{"file_id":"uuid-from-download","resolution":513}' \
  --output heightmap.png
```

### Test Unity Integration

1. **System Test** (Right-click TerrainLoader):
   - Test Paths
   - Test Python Installation (local only)
   - Test Dependencies (local only)
   - Test Backend API (backend mode)
   - Test API Config

2. **Integration Test**:
   - Load terrain for "New York"
   - Load terrain for "Grand Canyon"
   - Load terrain for "Mount Fuji"

## Troubleshooting

### Backend Won't Start

**Docker**:
```bash
docker-compose logs
docker ps  # Check if running
docker-compose down
docker-compose up --build
```

**Python**:
```bash
# Check dependencies
pip list | findstr rasterio
pip list | findstr Flask

# Reinstall
pip install -r requirements.txt --force-reinstall
```

### Unity Can't Connect to Backend

1. **Check backend is running**:
```bash
curl http://localhost:5000/health
```

2. **Check firewall** allows port 5000

3. **Check api_config.json**:
   - backendUrl is correct
   - useLocalAPI is true

4. **Check Unity console** for connection errors

### Heightmap Generation Fails

**Backend Mode**:
- Check backend logs: `docker-compose logs`
- Verify OpenTopography API key in `.env`
- Test /api/process-dem endpoint directly

**Local Mode**:
- Verify Python installed: `python --version`
- Verify rasterio: `python -c "import rasterio; print('OK')"`
- Check python script exists: `Assets/StreamingAssets/Scripts/dem_processor_alt.py`

### WebGL Build Errors

1. **Make sure** `useLocalAPI: true` in config
2. **Deploy backend** before testing
3. **Update** `backendUrl` to production URL
4. **Enable CORS** in backend for your WebGL domain

## Architecture

### Local Mode (Desktop Only)
```
Unity App
  ↓
Direct API Calls → OpenTopography, Nominatim
  ↓
Python Subprocess → dem_processor_alt.py
  ↓
Heightmap PNG → Unity Terrain
```

### Backend Mode (WebGL Compatible)
```
Unity App
  ↓
REST API Calls → Backend Flask Server
  ↓
Backend → OpenTopography API (download DEM)
  ↓
Backend → rasterio processing (heightmap)
  ↓
PNG Response → Unity App
  ↓
Unity Terrain
```

## Configuration Files

### api_config.json (Unity)
- Backend URL
- useLocalAPI toggle
- OpenTopography API key (fallback)
- Nominatim settings
- Cache settings

### .env (Backend)
- OPENTOPO_API_KEY
- FLASK_ENV
- Port settings

### docker-compose.yml (Backend)
- Service configuration
- Port mapping
- Volume mounts
- Health checks

## Performance Tips

1. **Use caching**: Enable cache in `api_config.json`
2. **Reduce resolution**: Use 513 instead of 2049 for faster processing
3. **Smaller radius**: Use 5km instead of 10km for smaller areas
4. **Backend caching**: Backend automatically caches DEM files temporarily

## Support

For issues:
1. Check Unity console logs
2. Check backend logs (`docker-compose logs`)
3. Test backend endpoints with curl
4. Verify API keys are valid
5. Check OpenTopography quota: https://portal.opentopography.org/

## License

Part of Plantopia Unity project.
