# Plantopia Architecture - Backend API Integration

## Overview

Plantopia now has a **complete backend API** that enables **WebGL deployment** and provides a robust, scalable architecture for terrain generation.

## What Was Created

### 1. Backend API Server (`/backend` folder)

```
backend/
├── app.py                  # Flask API with all endpoints
├── requirements.txt        # Python dependencies
├── Dockerfile             # Container definition
├── docker-compose.yml     # Docker orchestration
├── .env                   # Environment configuration
├── .env.example           # Environment template
├── .gitignore            # Git ignore rules
├── .dockerignore         # Docker ignore rules
├── README.md             # Backend documentation
├── test_api.py           # API test script
├── temp/                 # Temporary DEM files
└── cache/                # Cached heightmaps
```

### 2. API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/health` | GET | Health check |
| `/api/geocode` | POST | Convert location name to coordinates |
| `/api/download-dem` | POST | Download DEM from OpenTopography |
| `/api/process-dem` | POST | Convert DEM to heightmap PNG |
| `/api/process-dem-info` | POST | Get DEM metadata |
| `/api/cleanup` | POST | Delete temporary files |

### 3. Updated Unity Scripts

All scripts now support **dual mode**:
- **Backend API Mode** (WebGL compatible)
- **Direct Mode** (Desktop fallback)

**Modified Files:**
- `GeocodeManager.cs` - API geocoding with Nominatim fallback
- `DEMDownloader.cs` - API download with direct fallback
- `DEMProcessor.cs` - API processing with local Python fallback
- `api_config.json` - Backend configuration

### 4. Configuration

**`api_config.json`** now includes:
```json
{
  "backend": {
    "backendUrl": "http://localhost:5000",
    "useLocalAPI": true,
    "fallbackToDirect": true
  }
}
```

**Backend `.env`**:
```
OPENTOPO_API_KEY=your_key_here
```

## Architecture Comparison

### Before (Python Subprocess - Desktop Only)

```
Unity App
  ↓ Direct HTTP
OpenTopography API → Download DEM (GeoTIFF)
  ↓ Save to disk
Python Subprocess (dem_processor_alt.py)
  ↓ rasterio processing
Heightmap PNG
  ↓ Load
Unity Terrain
```

**Problems:**
- ❌ Python required on client machine
- ❌ WebGL cannot execute subprocess
- ❌ Requires rasterio installation
- ❌ Cross-platform issues

### After (Backend API - WebGL Compatible)

```
Unity App (Desktop/WebGL)
  ↓ REST API
Backend Flask Server
  ├─→ Geocode via Nominatim
  ├─→ Download DEM from OpenTopography
  └─→ Process with rasterio
  ↓ Return PNG
Unity Terrain
```

**Benefits:**
- ✅ Works in WebGL builds
- ✅ No client-side Python required
- ✅ Centralized processing
- ✅ Easier deployment
- ✅ Better caching
- ✅ Load balancing possible

## How It Works

### 1. Geocoding Flow

```
Unity: GeocodeManager.GeocodeLocation("Grand Canyon")
  ↓
Backend: POST /api/geocode {"location": "Grand Canyon"}
  ↓
Nominatim API Query
  ↓
Response: {"latitude": 36.09804, "longitude": -112.0963}
  ↓
Unity: LocationData created
```

### 2. DEM Download Flow

```
Unity: DEMDownloader.DownloadDEM(location)
  ↓
Backend: POST /api/download-dem
  {
    "latitude": 36.09804,
    "longitude": -112.0963,
    "radius_km": 10,
    "api_key": "..."
  }
  ↓
OpenTopography API
  ↓
Backend saves DEM → temp/dem_{uuid}.tif
  ↓
Response: {"file_id": "uuid-string"}
  ↓
Unity: Store file_id for processing
```

### 3. Heightmap Processing Flow

```
Unity: DEMProcessor.ProcessDEM(file_id)
  ↓
Backend: POST /api/process-dem {"file_id": "uuid", "resolution": 513}
  ↓
Backend: Load temp/dem_{uuid}.tif
  ↓
rasterio: Read elevation data
  ↓
scipy: Interpolate NaN values
  ↓
PIL: Resize to 513x513
  ↓
Convert to 16-bit PNG
  ↓
Response: PNG file (image/png)
  ↓
Unity: Save to Heightmaps/temp/
  ↓
Backend: POST /api/cleanup {"file_id": "uuid"}
  ↓
Unity: TerrainGenerator.GenerateTerrainFromHeightmap()
```

## Deployment Strategies

### Local Development

```bash
cd backend
python app.py
```

Unity → `http://localhost:5000`

### Docker Local

```bash
cd backend
docker-compose up -d
```

Unity → `http://localhost:5000`

### Cloud Deployment

#### Heroku
```bash
heroku create plantopia-api
heroku config:set OPENTOPO_API_KEY=your_key
git push heroku main
```

Unity → `https://plantopia-api.herokuapp.com`

#### AWS Elastic Beanstalk
```bash
eb create plantopia-api
eb setenv OPENTOPO_API_KEY=your_key
```

Unity → `http://plantopia-api.us-west-2.elasticbeanstalk.com`

#### Google Cloud Run
```bash
gcloud builds submit --tag gcr.io/PROJECT/plantopia
gcloud run deploy --image gcr.io/PROJECT/plantopia
```

Unity → `https://plantopia-xxxxx-uc.a.run.app`

#### DigitalOcean App Platform
- Push to GitHub
- Connect repo in DigitalOcean
- Auto-deploy from `backend/` folder

Unity → `https://plantopia-xxxxx.ondigitalocean.app`

## WebGL Build Process

### 1. Deploy Backend

```bash
# Choose deployment method (Heroku, AWS, etc.)
# Get production URL: https://your-api.com
```

### 2. Update Unity Config

Edit `Assets/StreamingAssets/Config/api_config.json`:
```json
{
  "backend": {
    "backendUrl": "https://your-api.com",
    "useLocalAPI": true
  }
}
```

### 3. Build WebGL

```
File → Build Settings
Platform: WebGL
Player Settings → 
  - Company Name: Your Company
  - Product Name: Plantopia
  - WebGL Memory Size: 512MB
Build and Run
```

### 4. Test

- Open WebGL build in browser
- Enter location: "Mount Everest"
- Click "Load Terrain"
- Watch browser console for API calls
- Terrain should generate

## Configuration Options

### api_config.json

| Setting | Values | Description |
|---------|--------|-------------|
| `backendUrl` | URL | Backend API server address |
| `useLocalAPI` | true/false | Use backend (true) or direct API calls (false) |
| `fallbackToDirect` | true/false | Fallback to direct API if backend fails |

### Backend .env

| Variable | Description |
|----------|-------------|
| `OPENTOPO_API_KEY` | OpenTopography API key (required) |
| `FLASK_ENV` | `development` or `production` |

### Docker Environment

```yaml
environment:
  - OPENTOPO_API_KEY=${OPENTOPO_API_KEY}
  - FLASK_ENV=production
ports:
  - "5000:5000"
```

## Testing

### Test Backend API

```bash
cd backend
python test_api.py
```

Expected output:
```
1. Testing health endpoint...
   ✓ Health check passed: healthy
2. Testing geocode endpoint...
   ✓ Geocoded 'Grand Canyon' to: (36.09804, -112.0963)
3. Testing DEM download...
   ✓ DEM downloaded. File ID: abc-123
4. Testing DEM processing...
   ✓ Heightmap generated: test_heightmap_abc-123.png
5. Testing cleanup...
   ✓ Cleanup completed: Cleaned up 2 file(s)

Passed: 5/5
✓ All tests passed!
```

### Test Unity Integration

1. Start backend: `docker-compose up`
2. Open Unity project
3. Select TerrainLoader GameObject
4. Right-click → Test Backend API Connection
5. Console should show: "✓ Backend API is reachable"
6. Enter "Grand Canyon"
7. Click "Load Terrain"
8. Watch console for full pipeline

## Performance Considerations

### Backend Optimization

1. **Gunicorn workers**: Configured for 4 workers
2. **Timeout**: 120 seconds for large DEM processing
3. **Memory**: Container uses ~512MB per worker
4. **Caching**: Temporary files auto-cleanup

### Unity Optimization

1. **Resolution**: Default 513x513 (adjust for performance)
2. **Radius**: Default 10km (reduce for faster processing)
3. **Caching**: Enable in api_config.json
4. **Progressive loading**: Load terrain asynchronously

## Security

### API Keys

- **Never commit** `.env` file to git
- **Use environment variables** in production
- **Rotate keys** periodically
- **Monitor usage** on OpenTopography portal

### CORS

Backend enables CORS for all origins (development).

For production, restrict in `app.py`:
```python
CORS(app, origins=["https://yourdomain.com"])
```

### Rate Limiting

Consider adding Flask-Limiter:
```python
from flask_limiter import Limiter

limiter = Limiter(app, key_func=get_remote_address)

@app.route('/api/geocode', methods=['POST'])
@limiter.limit("10 per minute")
def geocode():
    ...
```

## Monitoring

### Health Checks

```bash
# Docker health check (automatic)
docker-compose logs | grep health

# Manual check
curl http://localhost:5000/health
```

### Logs

```bash
# Docker logs
docker-compose logs -f

# Specific service
docker-compose logs -f plantopia-backend

# Python logs (local)
tail -f app.log
```

### Metrics

Consider adding:
- Prometheus for metrics
- Grafana for visualization
- Sentry for error tracking

## Scaling

### Horizontal Scaling

```yaml
# docker-compose.yml
services:
  plantopia-backend:
    deploy:
      replicas: 3
    ...
  
  nginx:
    image: nginx
    # Load balancer config
```

### Vertical Scaling

Increase resources:
```yaml
services:
  plantopia-backend:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
```

### Cloud Auto-Scaling

Most cloud platforms support auto-scaling:
- AWS ECS: Task auto-scaling
- Google Cloud Run: Automatic
- Heroku: Dyno scaling
- DigitalOcean: App scaling

## Cost Considerations

### OpenTopography API
- **Free tier**: 1000 requests/month
- **Rate limit**: Reasonable usage
- **Alternative**: Self-host DEM data

### Backend Hosting

| Provider | Cost (est.) | Notes |
|----------|-------------|-------|
| Heroku | $7-25/mo | Hobby/Production dynos |
| AWS EB | $15-50/mo | t2.small instance |
| Google Cloud Run | $5-20/mo | Pay per use |
| DigitalOcean | $5-12/mo | App Platform |

### Unity WebGL Hosting

| Provider | Cost |
|----------|------|
| GitHub Pages | Free |
| Netlify | Free tier |
| Vercel | Free tier |
| itch.io | Free |

## Troubleshooting

### Backend Won't Start

```bash
# Check logs
docker-compose logs

# Rebuild
docker-compose down
docker-compose up --build

# Check port
netstat -an | findstr :5000
```

### Unity Can't Connect

1. Verify backend running: `curl http://localhost:5000/health`
2. Check firewall allows port 5000
3. Verify `api_config.json` has correct URL
4. Check Unity console for errors

### Heightmap Generation Fails

1. Check backend logs: `docker-compose logs`
2. Verify API key in `.env`
3. Test endpoint: `curl -X POST http://localhost:5000/api/process-dem ...`
4. Check DEM file exists in `backend/temp/`

### WebGL Build Fails

1. Ensure `useLocalAPI: true` in config
2. Deploy backend first
3. Update `backendUrl` to production
4. Check browser console for CORS errors
5. Verify backend allows CORS for your domain

## Future Enhancements

### Backend
- [ ] Redis caching for DEM files
- [ ] PostgreSQL for metadata storage
- [ ] Celery for async task queue
- [ ] WebSocket for real-time progress
- [ ] Multiple DEM sources
- [ ] Terrain texture generation
- [ ] 3D model export

### Unity
- [ ] Progressive terrain streaming
- [ ] LOD system for large terrains
- [ ] Multiplayer synchronization
- [ ] Terrain editing tools
- [ ] Vegetation placement
- [ ] Water simulation

## Summary

You now have:

✅ **Complete backend API** in `/backend` folder  
✅ **Docker containerization** for easy deployment  
✅ **Updated Unity scripts** with API integration  
✅ **Dual-mode architecture** (API + direct fallback)  
✅ **WebGL compatibility** for browser deployment  
✅ **Comprehensive documentation** and guides  
✅ **Testing tools** for validation  
✅ **Production-ready** deployment options  

Your Plantopia app can now be deployed as:
- Desktop app (Windows/Mac/Linux)
- WebGL browser app
- Mobile app (with backend API)

All while maintaining the same terrain generation quality and features!
