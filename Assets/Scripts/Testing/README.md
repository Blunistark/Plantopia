# Testing Utilities

This folder contains test scripts to verify individual components of Plantopia.

## Test Scripts

### GeocodeTest.cs
Tests the geocoding functionality using OpenStreetMap.

**Usage:**
1. Attach to any GameObject in your scene
2. Set `testLocationName` in the Inspector
3. Check `runOnStart` to auto-run, or right-click the component and select "Run Geocode Test"

**What it tests:**
- OpenStreetMap API connectivity
- JSON parsing
- LocationData creation

### Example Test Locations:
- "Grand Canyon"
- "Mount Everest"
- "Tokyo Tower"
- "Eiffel Tower"
- "Yosemite Valley"
