@echo off
echo ========================================
echo Stopping Plantopia Backend...
echo ========================================
echo.

cd /d "%~dp0"

docker-compose down

if errorlevel 1 (
    echo.
    echo ERROR: Failed to stop backend
    pause
    exit /b 1
)

echo.
echo ✓ Backend stopped successfully!
echo.
pause
