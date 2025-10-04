# Plantopia Backend API

Flask-based REST API for processing Digital Elevation Model (DEM) data for Unity terrain generation.

## Features

- **Geocoding**: Convert location names to coordinates using OpenStreetMap Nominatim
- **DEM Download**: Fetch elevation data from OpenTopography API
- **Heightmap Processing**: Convert GeoTIFF DEM files to PNG heightmaps
- **CORS Support**: Full WebGL compatibility
- **Docker Support**: Easy containerized deployment
- **Health Monitoring**: Built-in health check endpoint

## API Endpoints

### Health Check
```
GET /health
Response: {"status": "healthy", "timestamp": "...", "version": "1.0.0"}
```

### Geocode Location
```
POST /api/geocode
Content-Type: application/json

{
  "location": "Grand Canyon"
}

Response:
{
  "latitude": 36.09804,
  "longitude": -112.0963,
  "display_name": "Grand Canyon, Coconino County, Arizona, United States",
  "boundingbox": [...]
}
```

### Download DEM
```
POST /api/download-dem
Content-Type: application/json

{
  "latitude": 36.09804,
  "longitude": -112.0963,
  "radius_km": 10,
  "dem_type": "SRTMGL1",
  "api_key": "your_opentopo_key"  // Optional if set in environment
}

Response:
{
  "file_id": "uuid-string",
  "message": "DEM downloaded successfully",
  "size_bytes": 1234567,
  "bbox": {...}
}
```

### Process DEM to Heightmap
```
POST /api/process-dem
Content-Type: application/json

{
  "file_id": "uuid-string",
  "resolution": 513
}

Response: PNG file (image/png)
```

### Get DEM Info
```
POST /api/process-dem-info
Content-Type: application/json

{
  "file_id": "uuid-string"
}

Response:
{
  "min_elevation": 1000.5,
  "max_elevation": 2500.8,
  "size": [1201, 1201],
  "bounds": {...},
  "crs": "EPSG:4326"
}
```

### Cleanup Files
```
POST /api/cleanup
Content-Type: application/json

{
  "file_id": "uuid-string"
}

Response:
{
  "message": "Cleaned up 2 file(s)",
  "deleted": ["dem_uuid.tif", "dem_uuid_heightmap.png"]
}
```

## Installation

### Local Development

1. **Install Python dependencies**:
```bash
pip install -r requirements.txt
```

2. **Set up environment variables**:
```bash
cp .env.example .env
# Edit .env and add your OpenTopography API key
```

3. **Run development server**:
```bash
python app.py
```

Server runs on `http://localhost:5000`

### Docker Deployment

1. **Build and run with Docker Compose**:
```bash
# Set your API key in .env file first
docker-compose up -d
```

2. **View logs**:
```bash
docker-compose logs -f
```

3. **Stop container**:
```bash
docker-compose down
```

### Manual Docker Build

```bash
# Build image
docker build -t plantopia-backend .

# Run container
docker run -d \
  -p 5000:5000 \
  -e OPENTOPO_API_KEY=your_key_here \
  --name plantopia-api \
  plantopia-backend
```

## Configuration

### Environment Variables

- `OPENTOPO_API_KEY`: Your OpenTopography API key (required)
- `FLASK_ENV`: Set to `production` for deployment
- `FLASK_APP`: Set to `app.py` (default)

### Supported DEM Types

- `SRTMGL1`: SRTM 30m resolution (default)
- `SRTMGL3`: SRTM 90m resolution
- `AW3D30`: ALOS World 3D 30m
- `SRTMGL1_E`: SRTM ellipsoidal heights

### Heightmap Resolutions

Valid resolutions (must be 2^n + 1 for Unity terrain):
- 129x129
- 257x257
- 513x513 (default)
- 1025x1025
- 2049x2049
- 4097x4097

## Deployment

### Production Considerations

1. **Use gunicorn** (included in Dockerfile):
```bash
gunicorn --bind 0.0.0.0:5000 --workers 4 --timeout 120 app:app
```

2. **Set up reverse proxy** (Nginx example):
```nginx
server {
    listen 80;
    server_name api.plantopia.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

3. **Enable HTTPS** with Let's Encrypt/Certbot

4. **Configure CORS** for your Unity WebGL domain in `app.py`:
```python
CORS(app, origins=["https://yourdomain.com"])
```

### Cloud Deployment Options

#### Heroku
```bash
heroku create plantopia-api
heroku config:set OPENTOPO_API_KEY=your_key
git push heroku main
```

#### AWS Elastic Beanstalk
```bash
eb init -p python-3.10 plantopia-backend
eb create plantopia-api-env
eb setenv OPENTOPO_API_KEY=your_key
```

#### Google Cloud Run
```bash
gcloud builds submit --tag gcr.io/PROJECT_ID/plantopia-backend
gcloud run deploy --image gcr.io/PROJECT_ID/plantopia-backend --platform managed
```

## Testing

### Test with curl

```bash
# Health check
curl http://localhost:5000/health

# Geocode
curl -X POST http://localhost:5000/api/geocode \
  -H "Content-Type: application/json" \
  -d '{"location": "Mount Everest"}'

# Download DEM
curl -X POST http://localhost:5000/api/download-dem \
  -H "Content-Type: application/json" \
  -d '{
    "latitude": 27.9881,
    "longitude": 86.9250,
    "radius_km": 5,
    "api_key": "your_key"
  }'

# Process DEM (use file_id from previous response)
curl -X POST http://localhost:5000/api/process-dem \
  -H "Content-Type: application/json" \
  -d '{"file_id": "uuid-here", "resolution": 513}' \
  --output heightmap.png
```

## Troubleshooting

### Common Issues

**GDAL installation fails**:
- On Ubuntu: `sudo apt-get install gdal-bin libgdal-dev`
- On macOS: `brew install gdal`
- Use Docker to avoid local GDAL setup

**API key errors**:
- Verify key is set: `echo $OPENTOPO_API_KEY`
- Check key is valid at OpenTopography portal
- Pass key in request body as fallback

**Timeout errors**:
- Large areas need more time
- Increase gunicorn timeout: `--timeout 180`
- Reduce radius_km in requests

**Memory errors**:
- Reduce heightmap resolution
- Increase Docker memory limit
- Process smaller areas

## Development

### Project Structure
```
backend/
├── app.py              # Main Flask application
├── requirements.txt    # Python dependencies
├── Dockerfile         # Container definition
├── docker-compose.yml # Docker orchestration
├── .env.example       # Environment template
├── .dockerignore      # Docker ignore rules
├── README.md          # This file
├── temp/              # Temporary DEM files
└── cache/             # Cached heightmaps
```

### Adding New Endpoints

1. Add route decorator to function in `app.py`
2. Implement error handling with try/except
3. Return JSON responses with proper status codes
4. Update this README with new endpoint docs

## License

Part of the Plantopia Unity project.

## Support

For issues or questions:
1. Check Unity console logs
2. Review backend logs: `docker-compose logs`
3. Test endpoints with curl
4. Verify OpenTopography API quota

## Credits

- OpenTopography for DEM data
- OpenStreetMap Nominatim for geocoding
- Flask and rasterio communities
