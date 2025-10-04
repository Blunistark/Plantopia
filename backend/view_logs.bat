@echo off
echo ========================================
echo Plantopia Backend - View Logs
echo ========================================
echo.
echo Press Ctrl+C to stop viewing logs
echo.

cd /d "%~dp0"

docker-compose logs -f
