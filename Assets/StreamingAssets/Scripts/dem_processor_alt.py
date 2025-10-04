"""
DEM to Heightmap Processor - GDAL-Free Version
Converts DEM (Digital Elevation Model) files to Unity-compatible heightmaps
This version uses rasterio as an alternative to GDAL (easier Windows installation)
"""

import sys
import os
import numpy as np
from PIL import Image

# Try to import GDAL, fall back to rasterio
GDAL_AVAILABLE = False
RASTERIO_AVAILABLE = False

try:
    from osgeo import gdal
    GDAL_AVAILABLE = True
    print("Using GDAL for DEM processing")
except ImportError:
    try:
        import rasterio
        from rasterio.warp import calculate_default_transform, reproject, Resampling
        RASTERIO_AVAILABLE = True
        print("Using rasterio for DEM processing (GDAL not available)")
    except ImportError:
        print("ERROR: Neither GDAL nor rasterio is installed.")
        print("Please install one of them:")
        print("  pip install rasterio  (easier on Windows)")
        print("  OR")
        print("  pip install gdal  (requires system GDAL installation)")
        sys.exit(1)


def process_dem_with_gdal(dem_path, output_path, resolution=513):
    """Process DEM using GDAL"""
    dataset = gdal.Open(dem_path)
    if dataset is None:
        print(f"ERROR: Could not open DEM file: {dem_path}")
        sys.exit(1)
    
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
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    image.save(output_path)
    
    print(f"Heightmap saved successfully: {output_path}")
    dataset = None
    return output_path


def process_dem_with_rasterio(dem_path, output_path, resolution=513):
    """Process DEM using rasterio (easier Windows installation)"""
    with rasterio.open(dem_path) as dataset:
        elevation_data = dataset.read(1)
        
        print(f"DEM shape: {elevation_data.shape}")
        print(f"Elevation range: {np.min(elevation_data)} to {np.max(elevation_data)}")
        
        # Handle nodata values
        nodata = dataset.nodata
        if nodata is not None:
            elevation_data = np.where(elevation_data == nodata, np.nan, elevation_data)
        
        # Normalize elevation data to 0-1 range
        min_elev = np.nanmin(elevation_data)
        max_elev = np.nanmax(elevation_data)
        
        if max_elev == min_elev:
            normalized = np.zeros_like(elevation_data)
        else:
            normalized = (elevation_data - min_elev) / (max_elev - min_elev)
        
        # Replace NaN with 0
        normalized = np.nan_to_num(normalized, nan=0.0)
        
        # Resize to target resolution
        from scipy import ndimage
        zoom_factor = (resolution / elevation_data.shape[0], resolution / elevation_data.shape[1])
        resized = ndimage.zoom(normalized, zoom_factor, order=1)
        
        # Convert to 16-bit grayscale image
        heightmap = (resized * 65535).astype(np.uint16)
        
        # Save as PNG
        image = Image.fromarray(heightmap, mode='I;16')
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        image.save(output_path)
        
        print(f"Heightmap saved successfully: {output_path}")
        return output_path


def process_dem_to_heightmap(dem_path, output_path, resolution=513):
    """
    Process DEM file and convert to heightmap PNG
    Automatically uses available library (GDAL or rasterio)
    
    Args:
        dem_path: Path to input DEM file (GeoTIFF)
        output_path: Path to output heightmap PNG
        resolution: Output heightmap resolution (default: 513)
    """
    print(f"Processing DEM: {dem_path}")
    print(f"Output path: {output_path}")
    print(f"Resolution: {resolution}")
    
    if GDAL_AVAILABLE:
        return process_dem_with_gdal(dem_path, output_path, resolution)
    elif RASTERIO_AVAILABLE:
        return process_dem_with_rasterio(dem_path, output_path, resolution)
    else:
        print("ERROR: No DEM processing library available")
        sys.exit(1)


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
