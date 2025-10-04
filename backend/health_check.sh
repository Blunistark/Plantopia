#!/bin/bash
# Plantopia Backend - Health Check Script

echo "🌱 Plantopia Backend Health Check"
echo "====================================="
echo ""

ERRORS=0

# Function to check and print result
check() {
    local name="$1"
    local command="$2"
    
    printf "%-40s" "$name: "
    
    if eval "$command" > /dev/null 2>&1; then
        echo "✅ PASS"
    else
        echo "❌ FAIL"
        ((ERRORS++))
    fi
}

# 1. Container checks
echo "📦 Container Status"
echo "-------------------------------------"
check "Backend container running" "docker ps | grep -q plantopia-api"
check "Database container running" "docker ps | grep -q plantopia-postgres"
echo ""

# 2. Health endpoints
echo "🏥 Health Endpoints"
echo "-------------------------------------"
check "Backend /health responding" "curl -sf http://localhost:5000/health"
check "Database accepting connections" "docker exec plantopia-postgres pg_isready -U plantopia_admin -q"
echo ""

# 3. n8n connectivity
echo "🔗 n8n Integration"
echo "-------------------------------------"
check "n8n main container exists" "docker ps | grep -q n8n-main"
check "Backend can reach n8n" "docker exec plantopia-api curl -sf http://n8n-main:5678/healthz"
echo ""

# 4. Network checks
echo "🌐 Network Configuration"
echo "-------------------------------------"
check "Shared network exists" "docker network ls | grep -q n8n-central_shared-apps-network"
check "Backend on shared network" "docker inspect plantopia-api | grep -q shared-apps-network"
check "Database on shared network" "docker inspect plantopia-postgres | grep -q shared-apps-network"
echo ""

# 5. Environment checks
echo "⚙️  Configuration"
echo "-------------------------------------"
check ".env file exists" "test -f .env"
check "OPENTOPO_API_KEY set" "grep -q 'OPENTOPO_API_KEY=' .env && ! grep -q 'OPENTOPO_API_KEY=$' .env"
check "POSTGRES_PASSWORD set" "grep -q 'POSTGRES_PASSWORD=' .env && ! grep -q 'POSTGRES_PASSWORD=$' .env"
check "N8N_BASE_URL set" "grep -q 'N8N_BASE_URL=' .env"
echo ""

# 6. Volume checks
echo "💾 Storage"
echo "-------------------------------------"
check "PostgreSQL volume exists" "docker volume ls | grep -q postgres_data"
check "Cache directory exists" "test -d cache"
check "Temp directory exists" "test -d temp"
echo ""

# 7. Log checks (warnings only)
echo "📋 Recent Logs (Last 50 lines)"
echo "-------------------------------------"

BACKEND_ERRORS=$(docker logs --tail 50 plantopia-api 2>&1 | grep -i "error" | wc -l | tr -d ' ')
POSTGRES_ERRORS=$(docker logs --tail 50 plantopia-postgres 2>&1 | grep -i "error" | wc -l | tr -d ' ')

if [ "$BACKEND_ERRORS" -eq 0 ]; then
    echo "Backend errors: ✅ None"
else
    echo "Backend errors: ⚠️  $BACKEND_ERRORS found (check logs)"
fi

if [ "$POSTGRES_ERRORS" -eq 0 ]; then
    echo "Database errors: ✅ None"
else
    echo "Database errors: ⚠️  $POSTGRES_ERRORS found (check logs)"
fi
echo ""

# 8. Summary
echo "====================================="
if [ $ERRORS -eq 0 ]; then
    echo "✅ All checks passed! System is healthy."
    echo ""
    echo "📊 Quick Stats:"
    docker stats --no-stream plantopia-api plantopia-postgres 2>/dev/null | tail -n +2
    exit 0
else
    echo "❌ $ERRORS check(s) failed!"
    echo ""
    echo "🔧 Troubleshooting:"
    echo "   1. View logs: docker logs -f plantopia-api"
    echo "   2. Check n8n: cd ~/docker/n8n-central && docker compose ps"
    echo "   3. Restart: docker compose restart"
    echo "   4. See: DEPLOYMENT_CHECKLIST.md"
    exit 1
fi
