"""
Image processing utilities for heightmap generation
"""

import numpy as np
from PIL import Image
from scipy import ndimage


def apply_gaussian_blur(heightmap, sigma=1.0):
    """
    Apply Gaussian blur to heightmap
    
    Args:
        heightmap: Input heightmap array
        sigma: Blur strength
        
    Returns:
        Blurred heightmap
    """
    return ndimage.gaussian_filter(heightmap, sigma=sigma)


def normalize_heightmap(heightmap, min_val=0.0, max_val=1.0):
    """
    Normalize heightmap to specified range
    
    Args:
        heightmap: Input heightmap array
        min_val: Minimum value
        max_val: Maximum value
        
    Returns:
        Normalized heightmap
    """
    h_min = np.min(heightmap)
    h_max = np.max(heightmap)
    
    if h_max == h_min:
        return np.full_like(heightmap, min_val)
    
    normalized = (heightmap - h_min) / (h_max - h_min)
    return normalized * (max_val - min_val) + min_val


def resize_heightmap(heightmap, target_size):
    """
    Resize heightmap to target size
    
    Args:
        heightmap: Input heightmap array
        target_size: (width, height) tuple
        
    Returns:
        Resized heightmap
    """
    zoom_factor = (target_size[1] / heightmap.shape[0], 
                   target_size[0] / heightmap.shape[1])
    return ndimage.zoom(heightmap, zoom_factor, order=1)


def save_heightmap_16bit(heightmap, output_path):
    """
    Save heightmap as 16-bit PNG
    
    Args:
        heightmap: Input heightmap array (0-1 range)
        output_path: Output file path
    """
    # Convert to 16-bit
    heightmap_16bit = (heightmap * 65535).astype(np.uint16)
    
    # Save as PNG
    image = Image.fromarray(heightmap_16bit, mode='I;16')
    image.save(output_path)


def save_heightmap_8bit(heightmap, output_path):
    """
    Save heightmap as 8-bit PNG
    
    Args:
        heightmap: Input heightmap array (0-1 range)
        output_path: Output file path
    """
    # Convert to 8-bit
    heightmap_8bit = (heightmap * 255).astype(np.uint8)
    
    # Save as PNG
    image = Image.fromarray(heightmap_8bit, mode='L')
    image.save(output_path)
