"""
DEM to Heightmap Processor
Converts DEM (Digital Elevation Model) files to Unity-compatible heightmaps
"""

import sys
import os
import numpy as np
from PIL import Image

try:
    from osgeo import gdal
except ImportError:
    print("ERROR: GDAL not installed. Please install GDAL: pip install gdal")
    sys.exit(1)


def process_dem_to_heightmap(dem_path, output_path, resolution=513):
    """
    Process DEM file and convert to heightmap PNG
    
    Args:
        dem_path: Path to input DEM file (GeoTIFF)
        output_path: Path to output heightmap PNG
        resolution: Output heightmap resolution (default: 513)
    """
    print(f"Processing DEM: {dem_path}")
    print(f"Output path: {output_path}")
    print(f"Resolution: {resolution}")
    
    # Open DEM file
    dataset = gdal.Open(dem_path)
    if dataset is None:
        print(f"ERROR: Could not open DEM file: {dem_path}")
        sys.exit(1)
    
    # Read elevation data
    band = dataset.GetRasterBand(1)
    elevation_data = band.ReadAsArray()
    
    print(f"DEM shape: {elevation_data.shape}")
    print(f"Elevation range: {np.min(elevation_data)} to {np.max(elevation_data)}")
    
    # Normalize elevation data to 0-1 range
    min_elev = np.min(elevation_data)
    max_elev = np.max(elevation_data)
    
    if max_elev == min_elev:
        normalized = np.zeros_like(elevation_data)
    else:
        normalized = (elevation_data - min_elev) / (max_elev - min_elev)
    
    # Resize to target resolution
    from scipy import ndimage
    zoom_factor = (resolution / elevation_data.shape[0], resolution / elevation_data.shape[1])
    resized = ndimage.zoom(normalized, zoom_factor, order=1)
    
    # Convert to 16-bit grayscale image
    heightmap = (resized * 65535).astype(np.uint16)
    
    # Save as PNG
    image = Image.fromarray(heightmap, mode='I;16')
    
    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    
    image.save(output_path)
    print(f"Heightmap saved successfully: {output_path}")
    
    # Close dataset
    dataset = None
    
    return output_path


def main():
    """Main entry point"""
    if len(sys.argv) < 3:
        print("Usage: python dem_processor.py <dem_file> <output_file> [resolution]")
        sys.exit(1)
    
    dem_path = sys.argv[1]
    output_path = sys.argv[2]
    resolution = int(sys.argv[3]) if len(sys.argv) > 3 else 513
    
    if not os.path.exists(dem_path):
        print(f"ERROR: DEM file not found: {dem_path}")
        sys.exit(1)
    
    try:
        process_dem_to_heightmap(dem_path, output_path, resolution)
        print("SUCCESS")
    except Exception as e:
        print(f"ERROR: {str(e)}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
