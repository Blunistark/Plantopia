@echo off
echo ========================================
echo Plantopia Backend - Quick Start
echo ========================================
echo.

cd /d "%~dp0"

echo Checking Docker...
docker --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker is not installed or not in PATH
    echo.
    echo Please install Docker Desktop from:
    echo https://www.docker.com/products/docker-desktop
    echo.
    pause
    exit /b 1
)

echo Docker found!
echo.

echo Starting Plantopia Backend...
echo.
docker-compose up -d

if errorlevel 1 (
    echo.
    echo ERROR: Failed to start backend
    echo Check the error messages above
    pause
    exit /b 1
)

echo.
echo ========================================
echo Backend started successfully!
echo ========================================
echo.
echo Backend API: http://localhost:5000
echo Health check: http://localhost:5000/health
echo.
echo To view logs: docker-compose logs -f
echo To stop: docker-compose down
echo.
echo Testing connection...
timeout /t 5 /nobreak >nul

curl -s http://localhost:5000/health >nul 2>&1
if errorlevel 1 (
    echo.
    echo WARNING: Backend is starting but not responding yet
    echo Wait a few seconds and try: curl http://localhost:5000/health
) else (
    echo.
    echo ✓ Backend is responding!
)

echo.
echo You can now use Unity to test terrain generation.
echo Press any key to exit...
pause >nul
