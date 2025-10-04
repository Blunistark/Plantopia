"""
Plantopia Backend API Server
Flask-based REST API for DEM data processing
Handles geocoding, DEM downloads, and heightmap generation
"""

from flask import Flask, request, jsonify, send_file
from flask_cors import CORS
import os
import requests
import rasterio
import numpy as np
from PIL import Image
from scipy.interpolate import griddata
import tempfile
import logging
from werkzeug.utils import secure_filename
import uuid
from datetime import datetime

app = Flask(__name__)
CORS(app)  # Enable CORS for Unity WebGL

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Configuration
UPLOAD_FOLDER = 'temp'
CACHE_FOLDER = 'cache'
ALLOWED_EXTENSIONS = {'tif', 'tiff'}
MAX_FILE_SIZE = 500 * 1024 * 1024  # 500MB

os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(CACHE_FOLDER, exist_ok=True)

app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
app.config['MAX_CONTENT_LENGTH'] = MAX_FILE_SIZE

# OpenTopography API configuration
OPENTOPO_API_KEY = os.environ.get('OPENTOPO_API_KEY', '')
OPENTOPO_BASE_URL = 'https://portal.opentopography.org/API/globaldem'


def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'timestamp': datetime.utcnow().isoformat(),
        'version': '1.0.0'
    })


@app.route('/api/geocode', methods=['POST'])
def geocode():
    """
    Geocode a location name to coordinates
    POST /api/geocode
    Body: {"location": "Grand Canyon"}
    Returns: {"latitude": 36.09804, "longitude": -112.0963, "display_name": "..."}
    """
    try:
        data = request.get_json()
        location = data.get('location')
        
        if not location:
            return jsonify({'error': 'Location parameter is required'}), 400
        
        # Use Nominatim API for geocoding
        url = 'https://nominatim.openstreetmap.org/search'
        params = {
            'q': location,
            'format': 'json',
            'limit': 1
        }
        headers = {
            'User-Agent': 'Plantopia Unity App/1.0'
        }
        
        response = requests.get(url, params=params, headers=headers, timeout=10)
        response.raise_for_status()
        
        results = response.json()
        
        if not results:
            return jsonify({'error': f'Location not found: {location}'}), 404
        
        result = results[0]
        
        return jsonify({
            'latitude': float(result['lat']),
            'longitude': float(result['lon']),
            'display_name': result.get('display_name', location),
            'boundingbox': result.get('boundingbox', [])
        })
        
    except requests.RequestException as e:
        logger.error(f"Geocoding request failed: {e}")
        return jsonify({'error': f'Geocoding request failed: {str(e)}'}), 500
    except Exception as e:
        logger.error(f"Geocoding error: {e}")
        return jsonify({'error': str(e)}), 500


@app.route('/api/download-dem', methods=['POST'])
def download_dem():
    """
    Download DEM data from OpenTopography
    POST /api/download-dem
    Body: {
        "latitude": 36.09804,
        "longitude": -112.0963,
        "radius_km": 10,
        "dem_type": "SRTMGL1"
    }
    Returns: {"file_id": "uuid", "message": "DEM downloaded successfully"}
    """
    try:
        data = request.get_json()
        latitude = data.get('latitude')
        longitude = data.get('longitude')
        radius_km = data.get('radius_km', 10)
        dem_type = data.get('dem_type', 'SRTMGL1')
        
        if latitude is None or longitude is None:
            return jsonify({'error': 'Latitude and longitude are required'}), 400
        
        # Calculate bounding box
        # Approximate: 1 degree latitude ≈ 111 km
        # 1 degree longitude varies with latitude
        lat_offset = radius_km / 111.0
        lon_offset = radius_km / (111.0 * np.cos(np.radians(latitude)))
        
        south = latitude - lat_offset
        north = latitude + lat_offset
        west = longitude - lon_offset
        east = longitude + lon_offset
        
        # Check API key
        api_key = OPENTOPO_API_KEY or data.get('api_key')
        if not api_key:
            return jsonify({'error': 'OpenTopography API key not configured'}), 500
        
        # Build OpenTopography API request
        params = {
            'demtype': dem_type,
            'south': south,
            'north': north,
            'west': west,
            'east': east,
            'outputFormat': 'GTiff',
            'API_Key': api_key
        }
        
        logger.info(f"Downloading DEM: {dem_type} for bbox ({south},{west}) to ({north},{east})")
        
        response = requests.get(OPENTOPO_BASE_URL, params=params, timeout=120)
        response.raise_for_status()
        
        # Generate unique file ID
        file_id = str(uuid.uuid4())
        output_path = os.path.join(UPLOAD_FOLDER, f'dem_{file_id}.tif')
        
        # Save DEM file
        with open(output_path, 'wb') as f:
            f.write(response.content)
        
        logger.info(f"DEM saved: {output_path} ({len(response.content)} bytes)")
        
        return jsonify({
            'file_id': file_id,
            'message': 'DEM downloaded successfully',
            'size_bytes': len(response.content),
            'bbox': {
                'south': south,
                'north': north,
                'west': west,
                'east': east
            }
        })
        
    except requests.RequestException as e:
        logger.error(f"DEM download failed: {e}")
        return jsonify({'error': f'DEM download failed: {str(e)}'}), 500
    except Exception as e:
        logger.error(f"DEM download error: {e}")
        return jsonify({'error': str(e)}), 500


@app.route('/api/process-dem', methods=['POST'])
def process_dem():
    """
    Process DEM file to heightmap PNG
    POST /api/process-dem
    Body: {
        "file_id": "uuid",
        "resolution": 513
    }
    Returns: File download (PNG heightmap)
    """
    try:
        data = request.get_json()
        file_id = data.get('file_id')
        resolution = data.get('resolution', 513)
        
        if not file_id:
            return jsonify({'error': 'file_id is required'}), 400
        
        # Validate resolution
        if resolution not in [129, 257, 513, 1025, 2049, 4097]:
            return jsonify({'error': 'Invalid resolution. Must be 2^n + 1'}), 400
        
        # Find DEM file
        dem_path = os.path.join(UPLOAD_FOLDER, f'dem_{file_id}.tif')
        if not os.path.exists(dem_path):
            return jsonify({'error': f'DEM file not found: {file_id}'}), 404
        
        logger.info(f"Processing DEM: {dem_path} to {resolution}x{resolution}")
        
        # Process DEM to heightmap
        heightmap_path = process_dem_to_heightmap(dem_path, resolution)
        
        # Return heightmap file
        return send_file(
            heightmap_path,
            mimetype='image/png',
            as_attachment=True,
            download_name=f'heightmap_{file_id}.png'
        )
        
    except Exception as e:
        logger.error(f"DEM processing error: {e}")
        return jsonify({'error': str(e)}), 500


def process_dem_to_heightmap(dem_path, resolution):
    """
    Convert DEM GeoTIFF to heightmap PNG using rasterio
    """
    try:
        # Read DEM with rasterio
        with rasterio.open(dem_path) as src:
            elevation_data = src.read(1)
            
            # Handle NoData values
            if src.nodata is not None:
                elevation_data = np.where(
                    elevation_data == src.nodata,
                    np.nan,
                    elevation_data
                )
            
            # Fill NaN values with interpolation
            if np.any(np.isnan(elevation_data)):
                mask = ~np.isnan(elevation_data)
                coords = np.array(np.nonzero(mask)).T
                values = elevation_data[mask]
                
                # Create grid for all points
                grid_y, grid_x = np.mgrid[0:elevation_data.shape[0], 0:elevation_data.shape[1]]
                
                # Interpolate
                elevation_data = griddata(
                    coords, values,
                    (grid_y, grid_x),
                    method='linear',
                    fill_value=np.nanmean(values) if len(values) > 0 else 0
                )
            
            # Normalize to 0-1 range
            min_elevation = np.nanmin(elevation_data)
            max_elevation = np.nanmax(elevation_data)
            
            if max_elevation > min_elevation:
                normalized = (elevation_data - min_elevation) / (max_elevation - min_elevation)
            else:
                normalized = np.zeros_like(elevation_data)
            
            # Resize to target resolution using PIL
            image = Image.fromarray((normalized * 255).astype(np.uint8), mode='L')
            image = image.resize((resolution, resolution), Image.LANCZOS)
            
            # Convert to 16-bit for better precision
            image_16bit = Image.fromarray((np.array(image) / 255.0 * 65535).astype(np.uint16))
            
            # Save heightmap
            output_path = dem_path.replace('.tif', '_heightmap.png')
            image_16bit.save(output_path, 'PNG')
            
            logger.info(f"Heightmap created: {output_path} (Range: {min_elevation:.2f}m to {max_elevation:.2f}m)")
            
            return output_path
            
    except Exception as e:
        logger.error(f"Error processing DEM to heightmap: {e}")
        raise


@app.route('/api/process-dem-info', methods=['POST'])
def process_dem_info():
    """
    Get DEM processing info without generating heightmap
    POST /api/process-dem-info
    Body: {"file_id": "uuid"}
    Returns: {"min_elevation": 1000, "max_elevation": 2500, "size": [1201, 1201]}
    """
    try:
        data = request.get_json()
        file_id = data.get('file_id')
        
        if not file_id:
            return jsonify({'error': 'file_id is required'}), 400
        
        dem_path = os.path.join(UPLOAD_FOLDER, f'dem_{file_id}.tif')
        if not os.path.exists(dem_path):
            return jsonify({'error': f'DEM file not found: {file_id}'}), 404
        
        # Read DEM metadata
        with rasterio.open(dem_path) as src:
            elevation_data = src.read(1)
            
            # Handle NoData
            if src.nodata is not None:
                elevation_data = np.where(
                    elevation_data == src.nodata,
                    np.nan,
                    elevation_data
                )
            
            min_elev = float(np.nanmin(elevation_data))
            max_elev = float(np.nanmax(elevation_data))
            
            return jsonify({
                'min_elevation': min_elev,
                'max_elevation': max_elev,
                'size': list(elevation_data.shape),
                'bounds': src.bounds._asdict(),
                'crs': str(src.crs)
            })
            
    except Exception as e:
        logger.error(f"Error getting DEM info: {e}")
        return jsonify({'error': str(e)}), 500


@app.route('/api/cleanup', methods=['POST'])
def cleanup():
    """
    Cleanup temporary files
    POST /api/cleanup
    Body: {"file_id": "uuid"}
    """
    try:
        data = request.get_json()
        file_id = data.get('file_id')
        
        if not file_id:
            return jsonify({'error': 'file_id is required'}), 400
        
        # Delete associated files
        patterns = [
            f'dem_{file_id}.tif',
            f'dem_{file_id}_heightmap.png'
        ]
        
        deleted = []
        for pattern in patterns:
            file_path = os.path.join(UPLOAD_FOLDER, pattern)
            if os.path.exists(file_path):
                os.remove(file_path)
                deleted.append(pattern)
                logger.info(f"Deleted: {file_path}")
        
        return jsonify({
            'message': f'Cleaned up {len(deleted)} file(s)',
            'deleted': deleted
        })
        
    except Exception as e:
        logger.error(f"Cleanup error: {e}")
        return jsonify({'error': str(e)}), 500


if __name__ == '__main__':
    # Development server
    app.run(host='0.0.0.0', port=5000, debug=True)
