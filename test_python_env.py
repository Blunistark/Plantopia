"""
Test script to verify rasterio installation and basic DEM processing
Run this to make sure everything is working before using in Unity
"""

import sys
import os

def test_imports():
    """Test that all required packages are importable"""
    print("=" * 60)
    print("Testing Python Package Imports")
    print("=" * 60)
    
    packages = [
        ('numpy', 'Array processing'),
        ('PIL', 'Image handling (Pillow)'),
        ('scipy', 'Scientific computing'),
        ('rasterio', 'GeoTIFF reading')
    ]
    
    all_ok = True
    
    for package_name, description in packages:
        try:
            __import__(package_name)
            print(f"✅ {package_name:15} - {description}")
        except ImportError as e:
            print(f"❌ {package_name:15} - MISSING! {str(e)}")
            all_ok = False
    
    print()
    return all_ok


def test_rasterio_capabilities():
    """Test rasterio specific features"""
    print("=" * 60)
    print("Testing Rasterio Capabilities")
    print("=" * 60)
    
    try:
        import rasterio
        print(f"✅ Rasterio version: {rasterio.__version__}")
        print(f"✅ GDAL version: {rasterio.__gdal_version__}")
        
        # List supported drivers
        drivers = rasterio.drivers.raster_driver_extensions()
        print(f"✅ Supported formats: GeoTIFF, {len(drivers)} total formats")
        
        return True
    except Exception as e:
        print(f"❌ Rasterio test failed: {str(e)}")
        return False


def test_create_sample_heightmap():
    """Create a simple test heightmap to verify image output works"""
    print("=" * 60)
    print("Testing Heightmap Generation")
    print("=" * 60)
    
    try:
        import numpy as np
        from PIL import Image
        
        # Create simple gradient heightmap
        resolution = 513
        heightmap = np.zeros((resolution, resolution), dtype=np.float32)
        
        for i in range(resolution):
            for j in range(resolution):
                heightmap[i, j] = (i + j) / (2 * resolution)
        
        # Convert to 16-bit
        heightmap_16bit = (heightmap * 65535).astype(np.uint16)
        
        # Save test image
        output_path = "test_heightmap.png"
        image = Image.fromarray(heightmap_16bit, mode='I;16')
        image.save(output_path)
        
        print(f"✅ Created test heightmap: {output_path}")
        print(f"✅ Size: {resolution}x{resolution}")
        print(f"✅ Format: 16-bit grayscale PNG")
        
        # Clean up
        if os.path.exists(output_path):
            os.remove(output_path)
            print(f"✅ Test file cleaned up")
        
        return True
    except Exception as e:
        print(f"❌ Heightmap generation test failed: {str(e)}")
        return False


def main():
    """Run all tests"""
    print("\n")
    print("╔" + "=" * 58 + "╗")
    print("║" + " " * 10 + "PLANTOPIA PYTHON ENVIRONMENT TEST" + " " * 15 + "║")
    print("╚" + "=" * 58 + "╝")
    print()
    
    results = []
    
    # Test imports
    results.append(("Package Imports", test_imports()))
    
    # Test rasterio
    results.append(("Rasterio Features", test_rasterio_capabilities()))
    
    # Test heightmap generation
    results.append(("Heightmap Generation", test_create_sample_heightmap()))
    
    # Summary
    print()
    print("=" * 60)
    print("TEST SUMMARY")
    print("=" * 60)
    
    all_passed = True
    for test_name, passed in results:
        status = "✅ PASS" if passed else "❌ FAIL"
        print(f"{test_name:30} {status}")
        if not passed:
            all_passed = False
    
    print("=" * 60)
    
    if all_passed:
        print()
        print("🎉 ALL TESTS PASSED!")
        print("Your Python environment is ready for Plantopia!")
        print()
        print("Next steps:")
        print("1. Get OpenTopography API key")
        print("2. Add it to Assets/StreamingAssets/Config/api_config.json")
        print("3. Set up Unity scene with UI")
        print("4. Start loading real-world terrains!")
        return 0
    else:
        print()
        print("⚠️  SOME TESTS FAILED")
        print("Please fix the issues above before using Plantopia")
        print()
        print("Common fixes:")
        print("  pip install -r Assets/StreamingAssets/Scripts/requirements-rasterio.txt")
        return 1


if __name__ == "__main__":
    sys.exit(main())
