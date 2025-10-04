# Plantopia - Quick Start Guide

## ✅ Step 1: Verify Python Setup

You've already completed this! ✓
- Rasterio installed
- All dependencies ready

## 🔑 Step 2: Get OpenTopography API Key

1. Go to https://opentopography.org/
2. Click "MyOpenTopo" → "Request an API Key"
3. Fill out the form (free for research/education)
4. Copy your API key

5. Open: `Assets/StreamingAssets/Config/api_config.json`
6. Replace `YOUR_API_KEY_HERE` with your actual key:
   ```json
   "apiKey": "your_actual_key_here"
   ```

## 🎨 Step 3: Create Unity Scene

### Option A: Quick Test (No UI needed)
1. Open Unity Editor
2. Create empty scene: `Assets/Scenes/TerrainLoaderMain.unity`
3. Create empty GameObject named "TesterObject"
4. Attach script: `Assets/Scripts/Testing/GeocodeTest.cs`
5. In Inspector:
   - Set "Test Location Name" to "Grand Canyon"
   - Check "Run On Start"
6. Press Play - check Console for results!

### Option B: Full UI Setup
1. Create new scene: `Assets/Scenes/TerrainLoaderMain.unity`
2. **Create UI Canvas:**
   - Right-click Hierarchy → UI → Canvas
   - Set Canvas Scaler to "Scale With Screen Size"

3. **Add Input Field:**
   - Right-click Canvas → UI → Input Field (TextMeshPro if available)
   - Name it "LocationInput"
   - Set placeholder text: "Enter location name..."

4. **Add Load Button:**
   - Right-click Canvas → UI → Button
   - Name it "LoadButton"
   - Set button text: "Load Terrain"

5. **Add Progress Bar:**
   - Right-click Canvas → UI → Slider
   - Name it "ProgressBar"
   - Set Min Value: 0, Max Value: 1
   - Disable "Interactable"

6. **Add Status Text:**
   - Right-click Canvas → UI → Text
   - Name it "StatusText"
   - Set text: "Ready"

7. **Create Controller:**
   - Create empty GameObject: "TerrainLoader"
   - Attach: `TerrainLoaderController.cs`
   - Attach: `GeocodeManager.cs`
   - Attach: `DEMDownloader.cs`
   - Attach: `DEMProcessor.cs`
   - Attach: `TerrainGenerator.cs`
   - Attach: `UIController.cs`
   - Attach: `ProgressController.cs`

8. **Wire References in Inspector:**
   - TerrainLoaderController:
     - Drag all component references
   - UIController:
     - Drag UI elements (InputField, Button, Text, Panels)
   - ProgressController:
     - Drag Slider and Text elements

## 🧪 Step 4: Test Individual Components

### Test 1: Geocoding
```
1. Use GeocodeTest.cs (see Option A above)
2. Expected output in Console:
   ✅ SUCCESS! Location: Grand Canyon
   Latitude: 36.0544
   Longitude: -112.1401
```

### Test 2: Python Script
```cmd
# Test the dem_processor_alt.py script
python Assets\StreamingAssets\Scripts\dem_processor_alt.py
# Should show usage instructions (not an error)
```

### Test 3: Full Pipeline
Once UI is set up:
1. Press Play
2. Enter "Yosemite Valley" (small area, faster)
3. Click "Load Terrain"
4. Watch Console and Progress bar

## 📋 Common Issues & Solutions

### Issue: "Location not found"
- ✅ Check internet connection
- ✅ Try different location name (be specific: "Grand Canyon" not just "canyon")
- ✅ Check Console for API errors

### Issue: "DEM download failed"
- ✅ Add OpenTopography API key to config
- ✅ Check API key is valid
- ✅ Try smaller area (reduce radius in LocationData)

### Issue: "Python script failed"
- ✅ Verify: `python -c "import rasterio; print('OK')"`
- ✅ Check Python is in PATH
- ✅ Set correct Python path in DEMProcessor.cs if needed

### Issue: "Terrain generation failed"
- ✅ Check heightmap file was created in StreamingAssets/Heightmaps/temp/
- ✅ Verify image is valid (open in image viewer)
- ✅ Check Unity Console for specific errors

## 🎯 What to Build Next

Once basic pipeline works, enhance with:
- **Better UI**: Material Design, loading animations
- **Terrain Texturing**: Auto-apply textures based on elevation
- **Camera Controller**: Fly-cam or orbit camera
- **Caching System**: Save downloaded DEMs and heightmaps
- **Batch Processing**: Load multiple terrains
- **Export Options**: Save terrains as assets
- **Quality Settings**: Let users choose resolution/quality

## 📚 Project Structure Reference

```
Assets/
├── Scenes/TerrainLoaderMain.unity  ← Your main scene
├── Scripts/
│   ├── Core/                       ← Main logic
│   ├── Geocoding/                  ← Location finding
│   ├── DEM/                        ← Terrain data
│   ├── UI/                         ← User interface
│   └── Testing/                    ← Test utilities ✨
├── StreamingAssets/
│   ├── Config/api_config.json      ← ADD YOUR API KEY HERE!
│   ├── Scripts/dem_processor_alt.py ← Python processor
│   ├── DEM/temp/                   ← Downloaded files
│   └── Heightmaps/temp/            ← Generated images
```

## 🆘 Need Help?

Check the Console output - it's very verbose and will tell you exactly what's happening at each step!
