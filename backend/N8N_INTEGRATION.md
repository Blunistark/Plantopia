# 🌱 Plantopia n8n Integration Guide

## Integration with Central n8n Automation Hub

This guide covers the integration between Plantopia backend and the central n8n instance at `n8n.hacksters.tech`.

---

## ✅ What's Been Configured

### 1. Docker Compose Updates
- ✅ Added PostgreSQL with pgvector support for vector embeddings
- ✅ Connected to `n8n-central_shared-apps-network`
- ✅ Added health checks for database
- ✅ Environment variables for n8n integration

### 2. Environment Variables
Created in `.env.example`:
- `N8N_BASE_URL` - Points to n8n container
- `N8N_WEBHOOK_AI_CHAT` - AI chatbot webhook path
- `N8N_WEBHOOK_DATA_FETCH` - External data fetching webhook
- `DATABASE_URL` - PostgreSQL connection with pgvector

### 3. Python n8n Client
Created `n8n_client.py` with methods:
- `chat_with_ai()` - AI assistance for plants/terrain
- `fetch_data()` - NASA/weather API orchestration via n8n
- `health_check()` - Verify n8n connectivity

---

## 🚀 Deployment Steps

### Step 1: Create .env File
```bash
cd ~/docker/apps/plantopia/backend
cp .env.example .env
# Edit .env if needed (passwords, API keys, etc.)
```

### Step 2: Verify Shared Network Exists
```bash
docker network ls | grep n8n-central_shared-apps-network

# If not present, create it:
docker network create n8n-central_shared-apps-network
```

### Step 3: Start Plantopia Stack
```bash
docker compose up -d
```

### Step 4: Verify Connectivity
```bash
# Check if backend can reach n8n
docker exec -it plantopia-api curl http://n8n-main:5678/healthz

# Expected: OK
```

---

## 📝 Create n8n Workflows

### Workflow 1: AI Chat Assistant

**Purpose:** Provide plant care advice, terrain analysis, location recommendations

**Setup:**
1. Go to `https://n8n.hacksters.tech`
2. Create new workflow: "Plantopia AI Chat"
3. Add nodes:

**Node 1: Webhook Trigger**
- Method: POST
- Path: `plantopia-ai-chat`
- Response: When Last Node Finishes

**Node 2: Extract Data (Code)**
```javascript
const userId = $input.item.json.body.userId;
const message = $input.item.json.body.message;
const context = $input.item.json.body.context || {};

return [{
  json: {
    userId,
    message,
    context,
    processedAt: new Date().toISOString()
  }
}];
```

**Node 3: Call Gemini AI (HTTP Request)**
- Method: POST
- URL: `https://generativelanguage.googleapis.com/v1/models/gemini-pro:generateContent`
- Authentication: Add your Gemini API key
- Body:
```json
{
  "contents": [{
    "parts": [{
      "text": "You are a plant and terrain expert for Plantopia app. User question: {{ $json.message }}\n\nContext: Location - {{ $json.context.location }}, Terrain type: {{ $json.context.terrainType }}"
    }]
  }]
}
```

**Node 4: Format Response (Code)**
```javascript
const aiText = $input.item.json.candidates[0].content.parts[0].text;

return [{
  json: {
    message: aiText,
    metadata: {
      model: "gemini-pro",
      timestamp: new Date().toISOString(),
      userId: $('Extract Data').item.json.userId
    }
  }
}];
```

**Node 5: Respond to Webhook**
- Return response from Node 4

**Save and Activate!**

---

### Workflow 2: NASA Data Fetcher

**Purpose:** Fetch soil moisture, climate data from NASA APIs

**Setup:**
1. Create new workflow: "Plantopia NASA Data Fetch"
2. Add nodes:

**Node 1: Webhook Trigger**
- Method: POST
- Path: `plantopia-data-fetch`

**Node 2: Extract Parameters (Code)**
```javascript
const dataType = $input.item.json.body.dataType;
const params = $input.item.json.body.parameters;

return [{
  json: {
    dataType,
    latitude: params.latitude,
    longitude: params.longitude,
    timestamp: new Date().toISOString()
  }
}];
```

**Node 3: Switch (Based on data type)**
- Branch 1: `dataType === 'nasa-soil'`
- Branch 2: `dataType === 'weather'`

**Node 4a: NASA SMAP API (HTTP Request)**
- URL: `https://power.larc.nasa.gov/api/temporal/daily/point`
- Query: `latitude={{ $json.latitude }}&longitude={{ $json.longitude }}&parameters=SOIL_MOISTURE`

**Node 4b: Weather API (HTTP Request)**
- Your preferred weather API

**Node 5: Merge + Format**
- Combine results and return

---

## 💻 Usage in App Code

### Example 1: AI Chat Endpoint

Add to `app.py`:

```python
from n8n_client import n8n_client
import asyncio

@app.route('/api/ai-chat', methods=['POST'])
def ai_chat():
    """
    AI chatbot for plant/terrain questions
    POST /api/ai-chat
    Body: {
        "user_id": "user123",
        "message": "What plants grow well in mountainous terrain?",
        "context": {
            "location": "Grand Canyon",
            "latitude": 36.09,
            "longitude": -112.09,
            "terrainType": "mountainous"
        }
    }
    """
    try:
        data = request.get_json()
        user_id = data.get('user_id', 'anonymous')
        message = data.get('message')
        context = data.get('context', {})
        
        if not message:
            return jsonify({'error': 'Message is required'}), 400
        
        # Call n8n AI workflow
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        ai_response = loop.run_until_complete(
            n8n_client.chat_with_ai(user_id, message, context)
        )
        
        return jsonify({
            'success': True,
            'response': ai_response.get('message'),
            'metadata': ai_response.get('metadata')
        })
        
    except Exception as e:
        logger.error(f"AI chat error: {e}")
        return jsonify({
            'success': False,
            'error': 'AI service unavailable'
        }), 500
```

### Example 2: Fetch NASA Data

```python
@app.route('/api/nasa-data', methods=['POST'])
def fetch_nasa_data():
    """
    Fetch NASA soil/climate data via n8n
    POST /api/nasa-data
    Body: {
        "latitude": 36.09,
        "longitude": -112.09,
        "data_type": "nasa-soil"
    }
    """
    try:
        data = request.get_json()
        latitude = data.get('latitude')
        longitude = data.get('longitude')
        data_type = data.get('data_type', 'nasa-soil')
        
        if latitude is None or longitude is None:
            return jsonify({'error': 'Coordinates required'}), 400
        
        # Call n8n data fetch workflow
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        nasa_data = loop.run_until_complete(
            n8n_client.fetch_data(data_type, {
                'latitude': latitude,
                'longitude': longitude
            })
        )
        
        return jsonify({
            'success': True,
            'data': nasa_data
        })
        
    except Exception as e:
        logger.error(f"NASA data fetch error: {e}")
        return jsonify({
            'success': False,
            'error': 'Data fetch failed'
        }), 500
```

---

## 🧪 Testing

### Test 1: Network Connectivity
```bash
docker exec -it plantopia-api curl http://n8n-main:5678/healthz
```

### Test 2: AI Chat (after creating n8n workflow)
```bash
curl -X POST http://localhost:5000/api/ai-chat \
  -H "Content-Type: application/json" \
  -d '{
    "user_id": "test_user",
    "message": "What plants grow in deserts?",
    "context": {
      "location": "Sahara",
      "terrainType": "desert"
    }
  }'
```

### Test 3: Direct n8n Webhook
```bash
docker exec -it plantopia-api curl -X POST \
  http://n8n-main:5678/webhook/plantopia-ai-chat \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "test",
    "message": "Test message",
    "context": {}
  }'
```

---

## 🗄️ PostgreSQL with pgvector

Your PostgreSQL instance supports vector embeddings for:
- Storing plant embeddings
- Similarity search for terrain types
- Location-based recommendations

### Enable pgvector Extension
```sql
-- Connect to database
CREATE EXTENSION IF NOT EXISTS vector;

-- Example: Store plant embeddings
CREATE TABLE plants (
  id SERIAL PRIMARY KEY,
  name TEXT,
  description TEXT,
  embedding vector(1536)  -- For OpenAI embeddings
);

-- Similarity search
SELECT name, description
FROM plants
ORDER BY embedding <-> '[0.1, 0.2, ...]'::vector
LIMIT 5;
```

---

## 📊 Monitoring

### View Logs
```bash
# Backend logs
docker logs -f plantopia-api

# Database logs
docker logs -f plantopia-postgres

# n8n logs
cd ~/docker/n8n-central
docker logs -f n8n-main
```

### Check n8n Executions
- Go to `https://n8n.hacksters.tech`
- Click "Executions" tab
- See all webhook calls and results

---

## 🔧 Troubleshooting

### Issue: "Cannot connect to n8n"
```bash
# 1. Check n8n is running
cd ~/docker/n8n-central
docker compose ps

# 2. Verify network
docker network inspect n8n-central_shared-apps-network

# 3. Check if plantopia is on network
docker inspect plantopia-api | grep shared-apps-network

# 4. Restart
docker compose restart
```

### Issue: Database Connection Failed
```bash
# Check postgres is healthy
docker exec -it plantopia-postgres pg_isready

# View database logs
docker logs plantopia-postgres
```

---

## 📋 Integration Checklist

- [x] Docker compose updated with PostgreSQL + pgvector
- [x] Shared network configuration added
- [x] Environment variables configured
- [x] n8n client created (`n8n_client.py`)
- [x] Requirements updated (httpx, psycopg2, pgvector)
- [ ] Copy `.env.example` to `.env`
- [ ] Start stack: `docker compose up -d`
- [ ] Create n8n workflows (AI Chat, Data Fetch)
- [ ] Test connectivity
- [ ] Add AI endpoint to `app.py`
- [ ] Test webhooks

---

## 🎯 Next Steps

1. **Create .env file** from .env.example
2. **Start the stack**: `docker compose up -d`
3. **Create n8n workflows** as described above
4. **Integrate endpoints** into app.py
5. **Test integration** with sample requests

---

**Integration Date:** October 5, 2025  
**n8n Instance:** https://n8n.hacksters.tech  
**Shared Network:** n8n-central_shared-apps-network
