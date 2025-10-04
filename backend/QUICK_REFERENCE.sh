#!/bin/bash
# Quick Reference - Common Commands for Plantopia Backend

# ========================================
# SETUP
# ========================================

# Initial setup
cp .env.example .env
chmod +x setup.sh
./setup.sh

# Create shared network manually
docker network create n8n-central_shared-apps-network

# ========================================
# START/STOP
# ========================================

# Start services
docker compose up -d

# Start with rebuild
docker compose up -d --build

# Stop services
docker compose down

# Stop and remove volumes
docker compose down -v

# Restart services
docker compose restart

# ========================================
# LOGS
# ========================================

# View all logs
docker compose logs -f

# Backend logs only
docker logs -f plantopia-api

# Database logs only
docker logs -f plantopia-postgres

# Last 100 lines
docker logs --tail 100 plantopia-api

# ========================================
# TESTING
# ========================================

# Health check
curl http://localhost:5000/health

# Test n8n connectivity
docker exec plantopia-api curl http://n8n-main:5678/healthz

# Test database
docker exec plantopia-postgres pg_isready -U plantopia_admin

# Geocode test
curl -X POST http://localhost:5000/api/geocode \
  -H "Content-Type: application/json" \
  -d '{"location": "Grand Canyon"}'

# ========================================
# DATABASE ACCESS
# ========================================

# Connect to PostgreSQL
docker exec -it plantopia-postgres psql -U plantopia_admin -d plantopia_db

# Create pgvector extension
docker exec -it plantopia-postgres psql -U plantopia_admin -d plantopia_db -c "CREATE EXTENSION IF NOT EXISTS vector;"

# List tables
docker exec -it plantopia-postgres psql -U plantopia_admin -d plantopia_db -c "\dt"

# ========================================
# DEBUGGING
# ========================================

# Inspect network
docker network inspect n8n-central_shared-apps-network

# Check container details
docker inspect plantopia-api

# Enter backend container
docker exec -it plantopia-api /bin/bash

# Enter database container
docker exec -it plantopia-postgres /bin/bash

# View environment variables
docker exec plantopia-api env | grep N8N

# ========================================
# CLEANUP
# ========================================

# Remove temp files
rm -rf temp/*.tif cache/*.png

# Clean docker cache
docker system prune -a

# Remove all stopped containers
docker container prune

# ========================================
# N8N INTEGRATION
# ========================================

# Test n8n webhook directly
docker exec plantopia-api curl -X POST \
  http://n8n-main:5678/webhook/plantopia-ai-chat \
  -H "Content-Type: application/json" \
  -d '{"userId":"test","message":"Hello","context":{}}'

# Check if on shared network
docker inspect plantopia-api | grep shared-apps-network

# Ping n8n from container
docker exec plantopia-api ping -c 3 n8n-main

# ========================================
# MONITORING
# ========================================

# Container stats
docker stats

# Specific container stats
docker stats plantopia-api plantopia-postgres

# List all containers
docker ps -a

# List volumes
docker volume ls

# Inspect volume
docker volume inspect backend_postgres_data

# ========================================
# USEFUL QUERIES
# ========================================

# Check disk usage
docker system df

# See running processes in container
docker top plantopia-api

# View port mappings
docker port plantopia-api

# ========================================
# QUICK FIXES
# ========================================

# Can't connect to n8n?
docker compose restart
docker exec plantopia-api curl http://n8n-main:5678/healthz

# Database issues?
docker compose down
docker volume rm backend_postgres_data
docker compose up -d

# Build issues?
docker compose down
docker compose build --no-cache
docker compose up -d

# Network issues?
docker network rm n8n-central_shared-apps-network
docker network create n8n-central_shared-apps-network
docker compose down && docker compose up -d

# ========================================
# HELP
# ========================================

# View docker compose config
docker compose config

# Validate docker compose file
docker compose config --quiet

# Show services
docker compose ps

# Show images
docker images | grep plantopia
