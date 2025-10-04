# 🎯 Quick Start - Run Your First Terrain!

## ✅ Pre-Flight Checklist (ALL DONE!)
- [x] Python 3.10.11 installed
- [x] All dependencies installed (numpy, Pillow, scipy, rasterio)
- [x] Folder structure created
- [x] API key configured
- [x] System tests passed

## 🚀 Run Your First Terrain in 3 Steps

### Step 1: Set Up the Scene (If Not Already Done)

1. **Create a new GameObject** named "TerrainLoader"
2. **Add these components** to it:
   - `TerrainLoaderController`
   - `GeocodeManager`
   - `DEMDownloader`
   - `DEMProcessor`
   - `TerrainGenerator`
   - `UIController` (optional, for status messages)
   - `ProgressController` (optional, for progress bar)

3. **In the Inspector**, wire up the references in `TerrainLoaderController`:
   - Drag `GeocodeManager` → geocodeManager field
   - Drag `DEMDownloader` → demDownloader field
   - Drag `DEMProcessor` → demProcessor field
   - Drag `TerrainGenerator` → terrainGenerator field

### Step 2: Add System Tester (For Easy Testing)

1. **Create another GameObject** named "SystemTester"
2. **Add component**: `SystemTester`
3. In Inspector, set:
   - Test Location: "Grand Canyon" (or any location)
   - Test On Start: false (optional - auto-test on play)

### Step 3: Run the Test!

**Right-click** on the `SystemTester` component in Inspector and choose:
- **"Test Full Pipeline"** - This will run the complete terrain generation!

## 📊 What You Should See

### Console Output (Successful Run):
```
=== STARTING FULL TERRAIN PIPELINE TEST ===
Location: Grand Canyon
Status: Finding coordinates for Grand Canyon...
Geocoding request: https://nominatim.openstreetmap.org/search?q=Grand+Canyon&format=json&limit=1
Geocoding response: [...]
Parsing JSON: {...}
Parsed - lat: 36.1069652, lon: -112.1129972, name: Grand Canyon
✓ Successfully created LocationData: Grand Canyon (36.10697, -112.113) - Radius: 10km
Location found: Grand Canyon (36.10697, -112.113) - Radius: 10km
Status: Downloading DEM for Grand Canyon...
OpenTopography API key loaded: 0cbdf615...
Downloading DEM from: https://portal.opentopography.org/API/globaldem?...
DEM downloaded successfully: D:/Apps/Unity/Plantopia/Assets/StreamingAssets/DEM/temp/dem_Grand Canyon_*.tif
Status: Processing elevation data...
Running Python command: python "D:/Apps/Unity/.../dem_processor_alt.py" "..." "..." 513
Using rasterio for DEM processing
Processing DEM: ...
DEM shape: (...)
Elevation range: ... to ...
Heightmap saved successfully: ...
DEM processing completed successfully
Status: Generating terrain...
Terrain generated successfully: Terrain_Grand Canyon
Status: Complete! ✓
```

### Timeline:
- **0-5 sec**: Geocoding
- **5-30 sec**: DEM Download (depends on internet speed)
- **30-45 sec**: Python Processing
- **45-47 sec**: Terrain Generation
- **Total**: ~45-60 seconds for first run

## 🎮 In the Scene View

After successful completion, you should see:
- A new **Terrain GameObject** in the Hierarchy
- The terrain visible in the **Scene view**
- Real elevation data from the Grand Canyon!

## 🔍 Troubleshooting

### If Geocoding Fails:
- Check internet connection
- Verify Nominatim API is accessible
- Try a different location name

### If DEM Download Fails:
- Verify API key in `api_config.json`
- Check OpenTopography rate limits (50/day free)
- Try a smaller area or different location
- Check console for HTTP error codes

### If Python Processing Fails:
- Test manually: `python Assets/StreamingAssets/Scripts/dem_processor_alt.py --help`
- Verify rasterio: `python -c "import rasterio; print('OK')"`
- Check file permissions in StreamingAssets folders
- Look for Python error messages in console

### If Terrain Generation Fails:
- Check if heightmap PNG was created in `StreamingAssets/Heightmaps/temp/`
- Verify the PNG is valid (open it in an image viewer)
- Check console for Unity errors

## 📝 Recommended Test Locations

**Start Small** (faster downloads):
- "Mount St. Helens" - Good test, small area
- "Half Dome" - Famous rock formation
- "Devils Tower" - Distinctive geological feature

**Larger Areas** (takes longer):
- "Grand Canyon" - Iconic, good detail
- "Yosemite Valley" - Beautiful terrain
- "Mount Rainier" - Great elevation changes

**Urban/Flat** (less interesting but fast):
- "Manhattan" - Will be very flat
- "Chicago" - Minimal elevation

## 🎨 Next Steps After First Success

1. **Adjust Quality**: Edit `terrain_presets.json` for different resolutions
2. **Add Textures**: Apply materials to terrain based on elevation
3. **Add Camera**: Create a fly-cam or orbit camera
4. **Build UI**: Create input field and button for location entry
5. **Add Caching**: Implement smart caching to avoid re-downloading
6. **Multiple Terrains**: Load and compare different locations

## 🆘 Still Having Issues?

Share the console output and I'll help debug! Include:
1. Which step failed (Geocoding/Download/Processing/Generation)
2. Any red error messages
3. The location name you're testing with

Good luck with your first terrain! 🏔️🚀
