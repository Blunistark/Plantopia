# Plantopia - Real-World Terrain Loader for Unity

A Unity project that loads real-world terrain data from Digital Elevation Models (DEMs) and generates 3D terrain. Now with **backend API support** for **WebGL deployment**!

## 🎯 Features

- **🌍 Location-based terrain generation**: Enter any location name worldwide
- **🗺️ OpenStreetMap integration**: Geocode location names to coordinates
- **⛰️ OpenTopography API**: Download real-world DEM data (SRTM, ALOS)
- **🐍 Python processing**: Convert DEM files to Unity-compatible heightmaps
- **🌐 WebGL compatible**: Deploy to browser with backend API
- **🐳 Docker support**: Easy containerized deployment
- **⚡ Dual-mode architecture**: API backend OR local processing
- **🎨 Customizable terrain**: Configure resolution, size, and quality presets

## 🏗️ Architecture

### Backend API Mode (WebGL Compatible)
```
Unity App (Desktop/WebGL)
    ↓
REST API → Flask Backend
    ↓
OpenTopography + Nominatim APIs
    ↓
Python/rasterio processing
    ↓
Heightmap PNG → Unity Terrain
```

### Direct Mode (Desktop Only)
```
Unity App (Desktop)
    ↓
Direct API calls → OpenTopography/Nominatim
    ↓
Local Python subprocess
    ↓
Heightmap PNG → Unity Terrain
```

## 📁 Project Structure

```
Plantopia/
├── backend/                 # 🆕 Flask API server
│   ├── app.py              # Main API server
│   ├── requirements.txt    # Python dependencies
│   ├── Dockerfile          # Container definition
│   ├── docker-compose.yml  # Docker orchestration
│   ├── test_api.py         # API testing script
│   └── README.md           # Backend documentation
├── Assets/
│   ├── Scenes/             # Unity scenes
│   ├── Scripts/            # C# scripts
│   │   ├── Core/           # Core functionality
│   │   ├── Geocoding/      # 🔄 API/Direct geocoding
│   │   ├── DEM/            # 🔄 API/Direct DEM processing
│   │   └── UI/             # User interface
│   ├── Prefabs/            # UI and terrain prefabs
│   ├── Materials/          # Materials and textures
│   ├── StreamingAssets/    # Runtime accessible files
│   │   ├── Scripts/        # Python processing scripts
│   │   ├── Heightmaps/     # Generated heightmaps
│   │   └── Config/         # 🔄 API configuration
│   ├── Editor/             # Editor tools
│   └── Resources/          # Runtime loadable resources
├── DEPLOYMENT_GUIDE.md     # 🆕 Deployment instructions
├── ARCHITECTURE.md         # 🆕 Architecture details
└── README.md               # This file
```

## 🚀 Quick Start

### Option 1: Backend API (Recommended for WebGL)

1. **Start the backend**:
```cmd
cd backend
docker-compose up -d
```

2. **Open Unity project**
3. **Configure**: Set `useLocalAPI: true` in `Assets/StreamingAssets/Config/api_config.json`
4. **Test**: Menu → Tools → Plantopia → Quick Setup
5. **Generate terrain**: Enter location name and click "Load Terrain"

### Option 2: Local Python (Desktop Only)

1. **Install Python dependencies**:
```cmd
pip install -r requirements.txt
```

2. **Open Unity project**
3. **Configure**: Set `useLocalAPI: false` in `api_config.json`
4. **Test**: Run System Tester from TerrainLoader inspector
5. **Generate terrain**: Enter location name and click "Load Terrain"

## 🔧 Setup Details

### Backend Setup (WebGL)


**Docker (Recommended)**:
```cmd
cd backend
docker-compose up -d
```

**Local Python**:
```cmd
cd backend
pip install -r requirements.txt
python app.py
```

**Test backend**:
```cmd
curl http://localhost:5000/health
# Or run: python backend/test_api.py
```

### Unity Setup

1. **Open project** in Unity 2022.3+
2. **Quick Setup**: Menu → Tools → Plantopia → Quick Setup
3. **Configure API**: Edit `Assets/StreamingAssets/Config/api_config.json`
```json
{
  "backend": {
    "backendUrl": "http://localhost:5000",
    "useLocalAPI": true
  },
  "openTopography": {
    "apiKey": "your_key_here"
  }
}
```

## 📖 Usage

### Basic Workflow

1. **Enter location**: "Grand Canyon", "Mount Everest", "New York"
2. **Click "Load Terrain"**
3. **System pipeline**:
   - 🔍 Geocode location → coordinates
   - ⬇️ Download DEM from OpenTopography
   - 🔄 Process DEM → heightmap PNG
   - 🏔️ Generate Unity terrain
4. **Result**: 3D terrain in your scene!

### Example Locations

- **Grand Canyon** - Deep canyon terrain
- **Mount Everest** - High altitude peaks
- **Death Valley** - Desert basin
- **Yosemite** - Mountain valley
- **Manhattan** - Urban terrain

## 🌐 WebGL Deployment

### 1. Deploy Backend

Choose a cloud provider:
```bash
# Heroku
heroku create plantopia-api
heroku config:set OPENTOPO_API_KEY=your_key
git push heroku main

# AWS, Google Cloud, DigitalOcean also supported
```

### 2. Update Unity Config

```json
{
  "backend": {
    "backendUrl": "https://plantopia-api.herokuapp.com",
    "useLocalAPI": true
  }
}
```

### 3. Build WebGL

```
File → Build Settings → WebGL → Build
```

### 4. Deploy WebGL Build

Upload to:
- GitHub Pages (free)
- Netlify (free)
- itch.io (free)
- Your own hosting

## 📚 Documentation

- **[DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)** - Complete deployment instructions
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture details
- **[backend/README.md](backend/README.md)** - Backend API documentation
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Testing procedures
- **[QUICK_START.md](QUICK_START.md)** - Quick start guide

## 🛠️ Development

### Testing

**Backend API**:
```cmd
cd backend
python test_api.py
```

**Unity System**:
1. Select TerrainLoader GameObject
2. Inspector → Right-click → Test All Systems
3. Check console for results

### Local Development

1. **Start backend**: `cd backend && docker-compose up`
2. **Open Unity**: Load project
3. **Test**: Generate terrain for test location
4. **Iterate**: Make changes, test again

## 🔄 API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/health` | GET | Health check |
| `/api/geocode` | POST | Location to coordinates |
| `/api/download-dem` | POST | Download DEM data |
| `/api/process-dem` | POST | DEM to heightmap |
| `/api/cleanup` | POST | Clean temp files |

See [backend/README.md](backend/README.md) for full API documentation.

## ⚙️ Configuration

### api_config.json

```json
{
  "backend": {
    "backendUrl": "http://localhost:5000",
    "useLocalAPI": true,
    "fallbackToDirect": true
  },
  "openTopography": {
    "apiKey": "your_key_here",
    "defaultDemType": "SRTMGL1"
  },
  "cache": {
    "enableDemCache": true,
    "enableHeightmapCache": true
  }
}
```

## 🐛 Troubleshooting

### Backend Won't Start
```cmd
docker-compose logs
docker-compose down && docker-compose up --build
```

### Unity Can't Connect
1. Check backend: `curl http://localhost:5000/health`
2. Verify `api_config.json` has correct URL
3. Check firewall allows port 5000

### Heightmap Generation Fails
1. Check backend logs: `docker-compose logs`
2. Verify API key in `backend/.env`
3. Test endpoint directly with curl

See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for more troubleshooting.

## 🎯 Features Roadmap

- [x] Backend API server
- [x] Docker containerization  
- [x] WebGL support
- [ ] Terrain texturing
- [ ] Vegetation placement
- [ ] Water simulation
- [ ] Progressive terrain streaming
- [ ] Multiplayer support

## 📄 License

This project is provided as-is for educational and development purposes.

## 🤝 Contributing

Contributions welcome! Feel free to:
- Report bugs
- Suggest features
- Submit pull requests

## 📞 Support

For issues:
1. Check documentation
2. Review troubleshooting guides
3. Check console logs (Unity + Backend)
4. Verify API keys and configuration

## 🙏 Credits

- **OpenTopography** for DEM data
- **OpenStreetMap Nominatim** for geocoding
- **rasterio** for geospatial processing
- **Flask** for backend API
- **Unity** for game engine

---

Made with ❤️ for real-world terrain generation in Unity
   - Process the DEM with Python
   - Generate Unity terrain

### Editor Tools

- **Tools > Plantopia > DEM Importer**: Import existing DEM files
- **Tools > Plantopia > Terrain Preview**: Preview terrain properties

### Configuration

Edit configuration files in `Assets/StreamingAssets/Config/`:

- `api_config.json`: API settings and keys
- `terrain_presets.json`: Terrain quality presets
- `dem_sources.json`: Available DEM data sources

## Scripts Overview

### Core Scripts
- `LocationData.cs`: Location data structure
- `TerrainMetadata.cs`: Terrain metadata
- `TerrainLoaderController.cs`: Main controller

### Geocoding
- `GeocodeManager.cs`: OpenStreetMap geocoding

### DEM Processing
- `DEMDownloader.cs`: Download DEM from OpenTopography
- `DEMProcessor.cs`: Python integration for processing
- `TerrainGenerator.cs`: Unity terrain generation

### UI
- `UIController.cs`: UI management
- `ProgressController.cs`: Progress bar updates

### Python Scripts
- `dem_processor.py`: Main DEM to heightmap converter
- `utils/gdal_helper.py`: GDAL utility functions
- `utils/image_utils.py`: Image processing utilities

## Troubleshooting

### Python not found
- Ensure Python is installed and in PATH
- Set `pythonExecutable` in `DEMProcessor.cs` if needed

### GDAL errors
- Install GDAL: `pip install gdal`
- On Windows, you may need to install OSGeo4W

### API errors
- Check your OpenTopography API key
- Verify internet connection
- Check API rate limits

## License

This project is for educational purposes.

## Credits

- DEM Data: OpenTopography, NASA SRTM
- Geocoding: OpenStreetMap Nominatim
- Python: GDAL, NumPy, Pillow, SciPy
