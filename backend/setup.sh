#!/bin/bash
# Plantopia Backend - Quick Setup Script

set -e

echo "🌱 Plantopia Backend - n8n Integration Setup"
echo "=============================================="
echo ""

# Step 1: Check if .env exists
if [ ! -f .env ]; then
    echo "📝 Creating .env from .env.example..."
    cp .env.example .env
    echo "✅ .env created. Please edit it with your actual credentials!"
    echo ""
else
    echo "✅ .env file already exists"
    echo ""
fi

# Step 2: Check if shared network exists
echo "🔍 Checking for shared network..."
if ! docker network ls | grep -q "n8n-central_shared-apps-network"; then
    echo "⚠️  Shared network not found. Creating it..."
    docker network create n8n-central_shared-apps-network
    echo "✅ Network created!"
else
    echo "✅ Shared network exists"
fi
echo ""

# Step 3: Check if n8n is running
echo "🔍 Checking if n8n is reachable..."
if docker ps | grep -q "n8n-main"; then
    echo "✅ n8n container is running"
else
    echo "⚠️  n8n container not found. Make sure central n8n stack is running:"
    echo "   cd ~/docker/n8n-central"
    echo "   docker compose up -d"
fi
echo ""

# Step 4: Build and start
echo "🚀 Starting Plantopia backend..."
docker compose up -d --build

echo ""
echo "⏳ Waiting for services to be healthy..."
sleep 10

# Step 5: Verify connectivity
echo ""
echo "🧪 Testing n8n connectivity..."
if docker exec plantopia-api curl -s http://n8n-main:5678/healthz > /dev/null 2>&1; then
    echo "✅ Successfully connected to n8n!"
else
    echo "❌ Cannot connect to n8n. Check if n8n is running."
fi

echo ""
echo "🧪 Testing backend health..."
if curl -s http://localhost:5000/health > /dev/null 2>&1; then
    echo "✅ Backend is healthy!"
else
    echo "⚠️  Backend not responding yet. Check logs: docker logs plantopia-api"
fi

echo ""
echo "=============================================="
echo "✅ Setup complete!"
echo ""
echo "📋 Next steps:"
echo "   1. Create n8n workflows (see N8N_INTEGRATION.md)"
echo "   2. Test endpoints: curl http://localhost:5000/health"
echo "   3. View logs: docker logs -f plantopia-api"
echo ""
echo "🔗 Useful commands:"
echo "   - View logs: docker logs -f plantopia-api"
echo "   - Restart: docker compose restart"
echo "   - Stop: docker compose down"
echo "   - Rebuild: docker compose up -d --build"
echo ""
