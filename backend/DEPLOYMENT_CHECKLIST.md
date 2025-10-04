# ✅ Plantopia n8n Integration - Deployment Checklist

## Pre-Deployment Checklist

### 1. Central n8n Setup
- [ ] Central n8n stack is running at `~/docker/n8n-central/`
- [ ] Can access n8n UI at `https://n8n.hacksters.tech`
- [ ] Shared network `n8n-central_shared-apps-network` exists
  ```bash
  docker network ls | grep n8n-central_shared-apps-network
  ```

### 2. Environment Configuration
- [ ] Copied `.env.example` to `.env`
  ```bash
  cp .env.example .env
  ```
- [ ] Updated `OPENTOPO_API_KEY` in `.env`
- [ ] Set secure `POSTGRES_PASSWORD` in `.env`
- [ ] Verified all required variables are set
  ```bash
  cat .env | grep -v "^#" | grep "="
  ```

### 3. File Permissions
- [ ] Made setup script executable
  ```bash
  chmod +x setup.sh
  ```
- [ ] Created cache and temp directories (auto-created by Docker)
- [ ] Verified docker-compose.yml syntax
  ```bash
  docker compose config --quiet && echo "✅ Valid" || echo "❌ Invalid"
  ```

---

## Deployment Checklist

### 4. Run Setup
- [ ] Executed setup script successfully
  ```bash
  ./setup.sh
  ```
  OR manually:
  ```bash
  docker network create n8n-central_shared-apps-network
  docker compose up -d --build
  ```

### 5. Verify Services
- [ ] Backend container is running
  ```bash
  docker ps | grep plantopia-api
  ```
- [ ] PostgreSQL container is running
  ```bash
  docker ps | grep plantopia-postgres
  ```
- [ ] No errors in backend logs
  ```bash
  docker logs plantopia-api | grep -i error
  ```
- [ ] No errors in database logs
  ```bash
  docker logs plantopia-postgres | grep -i error
  ```

### 6. Test Connectivity
- [ ] Backend health check passes
  ```bash
  curl http://localhost:5000/health
  # Expected: {"status":"healthy"...}
  ```
- [ ] Can reach n8n from backend
  ```bash
  docker exec plantopia-api curl http://n8n-main:5678/healthz
  # Expected: OK
  ```
- [ ] Database is accepting connections
  ```bash
  docker exec plantopia-postgres pg_isready -U plantopia_admin
  # Expected: accepting connections
  ```
- [ ] Backend is on shared network
  ```bash
  docker inspect plantopia-api | grep -A5 "shared-apps-network"
  ```

### 7. Test API Endpoints
- [ ] Geocoding works
  ```bash
  curl -X POST http://localhost:5000/api/geocode \
    -H "Content-Type: application/json" \
    -d '{"location": "Grand Canyon"}' | jq
  ```
- [ ] DEM download works (if you have OPENTOPO_API_KEY)
  ```bash
  curl -X POST http://localhost:5000/api/download-dem \
    -H "Content-Type: application/json" \
    -d '{
      "latitude": 36.09,
      "longitude": -112.09,
      "radius_km": 5
    }' | jq
  ```

---

## n8n Integration Checklist

### 8. Create n8n Workflows
- [ ] Logged into n8n UI at `https://n8n.hacksters.tech`
- [ ] Created "Plantopia AI Chat" workflow
  - [ ] Added Webhook trigger (path: `plantopia-ai-chat`)
  - [ ] Added data extraction node
  - [ ] Added AI/LLM node (Gemini, OpenAI, etc.)
  - [ ] Added response formatting node
  - [ ] Added webhook response node
  - [ ] Activated workflow
  - [ ] Tested with manual execution

- [ ] Created "Plantopia Data Fetch" workflow (optional)
  - [ ] Added Webhook trigger (path: `plantopia-data-fetch`)
  - [ ] Added parameter extraction
  - [ ] Added HTTP request nodes for external APIs
  - [ ] Added data merging/formatting
  - [ ] Added webhook response
  - [ ] Activated workflow

### 9. Test n8n Webhooks
- [ ] AI Chat webhook responds
  ```bash
  docker exec plantopia-api curl -X POST \
    http://n8n-main:5678/webhook/plantopia-ai-chat \
    -H "Content-Type: application/json" \
    -d '{
      "userId": "test_user",
      "message": "What plants grow in mountains?",
      "context": {}
    }' | jq
  ```
- [ ] Can see execution in n8n UI (Executions tab)
- [ ] Response format is correct

---

## Optional Integration Checklist

### 10. Add AI Endpoints to app.py (Optional)
- [ ] Imported n8n_client in app.py
- [ ] Added `/api/ai-chat` endpoint
- [ ] Added error handling for n8n calls
- [ ] Tested AI chat endpoint
  ```bash
  curl -X POST http://localhost:5000/api/ai-chat \
    -H "Content-Type: application/json" \
    -d '{
      "user_id": "test",
      "message": "What plants grow in deserts?",
      "context": {"location": "Sahara"}
    }' | jq
  ```

### 11. Database Setup (Optional)
- [ ] Enabled pgvector extension
  ```bash
  docker exec plantopia-postgres psql -U plantopia_admin -d plantopia_db \
    -c "CREATE EXTENSION IF NOT EXISTS vector;"
  ```
- [ ] Created tables for plant data (if needed)
- [ ] Set up vector similarity search (if needed)

---

## Production Readiness Checklist

### 12. Security
- [ ] Changed default PostgreSQL password
- [ ] API keys stored in .env (not in code)
- [ ] .env file is in .gitignore
- [ ] No sensitive data in docker-compose.yml
- [ ] Considered adding n8n API key for webhook auth

### 13. Monitoring
- [ ] Set up log rotation
  ```bash
  # Add to docker-compose.yml under backend service:
  logging:
    driver: "json-file"
    options:
      max-size: "10m"
      max-file: "3"
  ```
- [ ] Configured health check alerts
- [ ] Can view n8n execution history

### 14. Backup
- [ ] Database backup strategy planned
  ```bash
  docker exec plantopia-postgres pg_dump -U plantopia_admin plantopia_db > backup.sql
  ```
- [ ] Volume backup considered
- [ ] .env file backed up securely

### 15. Documentation
- [ ] Team knows how to access n8n UI
- [ ] Webhook URLs documented
- [ ] API endpoints documented
- [ ] Troubleshooting guide reviewed

---

## Post-Deployment Verification

### 16. Final Tests
- [ ] All containers running for 5+ minutes without restart
- [ ] Memory usage is acceptable (`docker stats`)
- [ ] No error logs appearing
- [ ] Can execute full DEM workflow (geocode → download → process)
- [ ] AI chat integration working (if implemented)

### 17. Performance
- [ ] Response times acceptable (<5s for most endpoints)
- [ ] n8n webhooks respond in <30s
- [ ] Database queries are fast
- [ ] No memory leaks observed

---

## Troubleshooting Reference

### Common Issues

**Cannot connect to n8n:**
```bash
# Check n8n is running
docker ps | grep n8n-main

# Restart both stacks
cd ~/docker/n8n-central && docker compose restart
cd ~/docker/apps/plantopia/backend && docker compose restart
```

**Database connection failed:**
```bash
# Check credentials
cat .env | grep POSTGRES

# Check database is healthy
docker logs plantopia-postgres

# Restart database
docker compose restart plantopia-postgres
```

**Webhook 404 error:**
- Check workflow is activated in n8n UI
- Verify webhook path matches .env variable
- Check n8n executions for errors

**Container keeps restarting:**
```bash
# View logs
docker logs plantopia-api

# Check resources
docker stats

# Rebuild
docker compose down
docker compose up -d --build
```

---

## Success Criteria

✅ All checkboxes above are checked
✅ Backend responds to health checks
✅ Can connect to n8n from backend container
✅ PostgreSQL is healthy and accessible
✅ At least one n8n workflow created and working
✅ No errors in logs
✅ Services restart automatically on failure
✅ Team can access and use the system

---

## Quick Health Check Command

Run this to verify everything is working:

```bash
#!/bin/bash
echo "🔍 Plantopia Health Check"
echo "========================="
echo ""

echo "1. Backend Health:"
curl -s http://localhost:5000/health | jq -r '.status' && echo "✅" || echo "❌"

echo ""
echo "2. n8n Connectivity:"
docker exec plantopia-api curl -s http://n8n-main:5678/healthz && echo "✅" || echo "❌"

echo ""
echo "3. Database Status:"
docker exec plantopia-postgres pg_isready -U plantopia_admin && echo "✅" || echo "❌"

echo ""
echo "4. Containers Running:"
docker ps | grep -E "plantopia-api|plantopia-postgres" | wc -l | grep -q "2" && echo "✅ 2/2 running" || echo "❌ Not all running"

echo ""
echo "========================="
```

Save this as `health_check.sh`, make executable: `chmod +x health_check.sh`, run: `./health_check.sh`

---

**Checklist Version:** 1.0  
**Last Updated:** October 5, 2025  
**Status:** Ready for deployment
