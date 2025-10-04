# Plantopia Quick Start & Testing Guide

## ✅ System Verification

### Current Status
- ✓ Python installed
- ✓ Rasterio installed successfully
- ✓ All Python dependencies ready
- ⚠ JSON parsing issue (FIXED - needs Unity recompile)

## 🧪 Testing in Unity

### Step 1: Verify the Fix
The JSON parsing has been fixed. Now when you run the test:
1. Unity will recompile the scripts automatically
2. Try the geocoding test again
3. You should see detailed debug output like:
   - "Parsing JSON: ..."
   - "Parsed - lat: X, lon: Y, name: ..."
   - "✓ Successfully created LocationData: ..."

### Step 2: Use the System Tester
I've created a `SystemTester.cs` component. Add it to your scene:

1. Create an empty GameObject named "SystemTester"
2. Add the `SystemTester` component
3. Right-click the component in Inspector
4. Select from the context menu:
   - **Run All Tests** - Complete system check
   - **Test Paths** - Verify folder structure
   - **Test Python Installation** - Check Python & packages
   - **Test API Config** - Verify configuration
   - **Test Geocoding** - Test the geocoding pipeline

### Step 3: Expected Console Output

**Successful Geocoding:**
```
Geocoding request: https://nominatim.openstreetmap.org/search?q=New+York&format=json&limit=1
Geocoding response: [{"place_id":331232479,...}]
Parsing JSON: {"place_id":331232479,"licence":"Data...
Parsed - lat: 40.7127281, lon: -74.0060152, name: City of New York
✓ Successfully created LocationData: City of New York (40.71273, -74.00602) - Radius: 10km
Location found: City of New York (40.71273, -74.00602) - Radius: 10km
Status: Downloading DEM for City of New York...
```

## 🔧 What Was Fixed

### The Issue
Unity's `JsonUtility.FromJson()` requires class fields to be **public**. The original `NominatimResult` class had private fields.

### The Fix
```csharp
// Before (WRONG)
private class NominatimResult
{
    public string lat;  // This won't work!
}

// After (CORRECT)
public class NominatimResult  // Class must be public
{
    public string lat;         // Fields are already public
}
```

## 📋 Next Steps After Testing

Once geocoding works, you'll need:

### 1. OpenTopography API Key
- Sign up: https://opentopography.org/
- Get free API key
- Edit: `Assets/StreamingAssets/Config/api_config.json`
- Replace `YOUR_API_KEY_HERE` with your actual key

### 2. Complete a Full Test
Test the entire pipeline with a small location:
1. Enter "Mount St. Helens" (small, good test data)
2. Watch the console for each step:
   - ✓ Geocoding
   - ✓ DEM Download
   - ✓ Python Processing
   - ✓ Terrain Generation

### 3. Troubleshooting

**If geocoding still fails:**
- Check console for "Parsing JSON:" messages
- Verify Unity recompiled the scripts (check timestamp)
- Try clearing Unity's script cache: Edit > Preferences > Clear Cache

**If DEM download fails:**
- Check your API key in api_config.json
- Verify internet connection
- Check OpenTopography rate limits (50 requests/day for free tier)

**If Python processing fails:**
- Run: `python Assets/StreamingAssets/Scripts/dem_processor_alt.py --help`
- Test rasterio: `python -c "import rasterio; print('OK')"`
- Check file permissions in StreamingAssets folders

## 🎯 Quick Commands

### Test Python Environment
```cmd
python --version
python -c "import rasterio, numpy, scipy; print('All dependencies OK')"
```

### Test Python Script Directly
```cmd
cd Assets/StreamingAssets/Scripts
python dem_processor_alt.py test_dem.tif output.png 513
```

### View Unity Console Logs
In Unity: Window > General > Console
- Filter by: Collapse, Clear on Play, Error Pause

## 📊 Expected Timeline

- ✅ **Step 1: Geocoding** - Should work NOW after recompile (~5 seconds)
- ⏳ **Step 2: DEM Download** - Depends on API key + internet (~10-30 seconds)
- ⏳ **Step 3: Python Processing** - First time may be slow (~5-15 seconds)
- ⏳ **Step 4: Terrain Generation** - Fast, Unity native (~1-2 seconds)

**Total:** About 30-60 seconds for first complete run

## 🆘 Need Help?

If you see any errors, share:
1. The exact error message from Unity Console
2. Which step it failed at (Geocoding/Download/Processing/Generation)
3. Any red or yellow warnings before the error

Good luck! The geocoding should work now after Unity recompiles. 🚀
