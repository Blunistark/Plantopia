"""
GDAL helper functions for DEM processing
"""

from osgeo import gdal, osr
import numpy as np


def get_dem_info(dem_path):
    """
    Get information about a DEM file
    
    Args:
        dem_path: Path to DEM file
        
    Returns:
        dict: DEM information
    """
    dataset = gdal.Open(dem_path)
    if dataset is None:
        return None
    
    band = dataset.GetRasterBand(1)
    geotransform = dataset.GetGeoTransform()
    projection = dataset.GetProjection()
    
    info = {
        'width': dataset.RasterXSize,
        'height': dataset.RasterYSize,
        'bands': dataset.RasterCount,
        'min_elevation': band.GetMinimum(),
        'max_elevation': band.GetMaximum(),
        'nodata_value': band.GetNoDataValue(),
        'geotransform': geotransform,
        'projection': projection
    }
    
    dataset = None
    return info


def reproject_dem(input_path, output_path, target_srs='EPSG:4326'):
    """
    Reproject DEM to target spatial reference system
    
    Args:
        input_path: Input DEM path
        output_path: Output DEM path
        target_srs: Target SRS (default: WGS84)
    """
    src_ds = gdal.Open(input_path)
    
    # Define target SRS
    dst_srs = osr.SpatialReference()
    dst_srs.ImportFromEPSG(int(target_srs.split(':')[1]))
    
    # Reproject
    gdal.Warp(output_path, src_ds, dstSRS=dst_srs.ExportToWkt())
    
    src_ds = None


def clip_dem(input_path, output_path, bounds):
    """
    Clip DEM to bounding box
    
    Args:
        input_path: Input DEM path
        output_path: Output DEM path
        bounds: (min_lon, min_lat, max_lon, max_lat)
    """
    src_ds = gdal.Open(input_path)
    
    # Clip using translate
    gdal.Translate(output_path, src_ds, 
                   projWin=[bounds[0], bounds[3], bounds[2], bounds[1]])
    
    src_ds = None
