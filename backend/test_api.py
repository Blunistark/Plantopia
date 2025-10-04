"""
Test script for Plantopia Backend API
Run this to verify all endpoints are working
"""

import requests
import json
import sys

BACKEND_URL = "http://localhost:5000"

def test_health():
    """Test health endpoint"""
    print("\n1. Testing health endpoint...")
    try:
        response = requests.get(f"{BACKEND_URL}/health")
        response.raise_for_status()
        data = response.json()
        print(f"   ✓ Health check passed: {data['status']}")
        return True
    except Exception as e:
        print(f"   ✗ Health check failed: {e}")
        return False

def test_geocode():
    """Test geocoding"""
    print("\n2. Testing geocode endpoint...")
    try:
        response = requests.post(
            f"{BACKEND_URL}/api/geocode",
            json={"location": "Grand Canyon"},
            headers={"Content-Type": "application/json"}
        )
        response.raise_for_status()
        data = response.json()
        print(f"   ✓ Geocoded 'Grand Canyon' to: ({data['latitude']}, {data['longitude']})")
        print(f"     Display name: {data['display_name']}")
        return True
    except Exception as e:
        print(f"   ✗ Geocode failed: {e}")
        return False

def test_download_dem(api_key):
    """Test DEM download"""
    print("\n3. Testing DEM download...")
    try:
        response = requests.post(
            f"{BACKEND_URL}/api/download-dem",
            json={
                "latitude": 36.09804,
                "longitude": -112.0963,
                "radius_km": 5,
                "dem_type": "SRTMGL1",
                "api_key": api_key
            },
            headers={"Content-Type": "application/json"}
        )
        response.raise_for_status()
        data = response.json()
        print(f"   ✓ DEM downloaded. File ID: {data['file_id']}")
        print(f"     Size: {data['size_bytes'] / 1024:.2f} KB")
        return data['file_id']
    except Exception as e:
        print(f"   ✗ DEM download failed: {e}")
        return None

def test_process_dem(file_id):
    """Test DEM processing"""
    print("\n4. Testing DEM processing...")
    try:
        response = requests.post(
            f"{BACKEND_URL}/api/process-dem",
            json={
                "file_id": file_id,
                "resolution": 513
            },
            headers={"Content-Type": "application/json"}
        )
        response.raise_for_status()
        
        # Save heightmap
        with open(f"test_heightmap_{file_id}.png", "wb") as f:
            f.write(response.content)
        
        print(f"   ✓ Heightmap generated: test_heightmap_{file_id}.png")
        print(f"     Size: {len(response.content) / 1024:.2f} KB")
        return True
    except Exception as e:
        print(f"   ✗ DEM processing failed: {e}")
        return False

def test_cleanup(file_id):
    """Test cleanup"""
    print("\n5. Testing cleanup...")
    try:
        response = requests.post(
            f"{BACKEND_URL}/api/cleanup",
            json={"file_id": file_id},
            headers={"Content-Type": "application/json"}
        )
        response.raise_for_status()
        data = response.json()
        print(f"   ✓ Cleanup completed: {data['message']}")
        return True
    except Exception as e:
        print(f"   ✗ Cleanup failed: {e}")
        return False

def main():
    print("=" * 60)
    print("Plantopia Backend API Test Suite")
    print("=" * 60)
    
    # Get API key
    api_key = input("\nEnter your OpenTopography API key (or press Enter for demo): ").strip()
    if not api_key:
        api_key = "0cbdf6155fbd19e73bae8dd14047be8d"  # Default from config
    
    results = []
    
    # Test health
    results.append(test_health())
    
    # Test geocode
    results.append(test_geocode())
    
    # Test DEM download
    file_id = test_download_dem(api_key)
    results.append(file_id is not None)
    
    # Test DEM processing (if download succeeded)
    if file_id:
        results.append(test_process_dem(file_id))
        results.append(test_cleanup(file_id))
    else:
        print("\n   ⚠ Skipping processing and cleanup tests (no file ID)")
        results.extend([False, False])
    
    # Summary
    print("\n" + "=" * 60)
    print("Test Summary")
    print("=" * 60)
    passed = sum(results)
    total = len(results)
    print(f"Passed: {passed}/{total}")
    
    if passed == total:
        print("\n✓ All tests passed! Backend is working correctly.")
        return 0
    else:
        print(f"\n✗ {total - passed} test(s) failed. Check output above.")
        return 1

if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("\n\nTest interrupted by user")
        sys.exit(1)
