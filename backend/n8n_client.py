"""
n8n Client Service for Plantopia
Handles communication with central n8n automation hub
"""

import os
import httpx
from typing import Dict, Any, Optional
from datetime import datetime
import logging

logger = logging.getLogger(__name__)


class N8NClient:
    """Client for interacting with n8n webhooks"""
    
    def __init__(self):
        self.base_url = os.getenv("N8N_BASE_URL", "http://n8n-main:5678")
        self.timeout = int(os.getenv("N8N_TIMEOUT", "30000")) / 1000
        self.api_key = os.getenv("N8N_API_KEY")
        
    async def call_webhook(
        self,
        webhook_path: str,
        payload: Dict[str, Any],
        headers: Optional[Dict[str, str]] = None
    ) -> Dict[str, Any]:
        """
        Call n8n webhook
        
        Args:
            webhook_path: Webhook path (e.g., '/webhook/plantopia-ai-chat')
            payload: Data to send
            headers: Additional headers
            
        Returns:
            Response data from n8n workflow
        """
        try:
            url = f"{self.base_url}{webhook_path}"
            
            request_headers = {
                "Content-Type": "application/json"
            }
            
            if self.api_key:
                request_headers["X-N8N-API-KEY"] = self.api_key
                
            if headers:
                request_headers.update(headers)
            
            logger.info(f"📤 Calling n8n webhook: {url}")
            
            async with httpx.AsyncClient() as client:
                response = await client.post(
                    url,
                    json=payload,
                    headers=request_headers,
                    timeout=self.timeout
                )
                response.raise_for_status()
                
                logger.info(f"✅ n8n response received ({response.status_code})")
                return response.json()
                
        except httpx.HTTPStatusError as e:
            logger.error(f"❌ n8n HTTP error: {e.response.status_code}")
            raise Exception(f"n8n error: {e.response.status_code}")
        except httpx.ConnectError:
            logger.error("❌ Cannot connect to n8n")
            raise Exception("Cannot connect to n8n. Is it running?")
        except Exception as e:
            logger.error(f"❌ n8n request failed: {str(e)}")
            raise Exception(f"n8n request failed: {str(e)}")
    
    async def chat_with_ai(
        self,
        user_id: str,
        message: str,
        context: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        """
        Call AI chatbot workflow for plant/terrain assistance
        
        Args:
            user_id: User identifier
            message: User's message/question
            context: Additional context (location, terrain data, etc.)
            
        Returns:
            AI response with guidance
        """
        webhook_path = os.getenv("N8N_WEBHOOK_AI_CHAT", "/webhook/plantopia-ai-chat")
        return await self.call_webhook(webhook_path, {
            "userId": user_id,
            "message": message,
            "context": context or {},
            "timestamp": datetime.utcnow().isoformat()
        })
    
    async def fetch_data(
        self,
        data_type: str,
        parameters: Dict[str, Any]
    ) -> Dict[str, Any]:
        """
        Fetch external data via n8n workflow (NASA APIs, weather, etc.)
        
        Args:
            data_type: Type of data to fetch (e.g., 'nasa-soil', 'weather')
            parameters: Query parameters (latitude, longitude, etc.)
            
        Returns:
            Fetched and processed data
        """
        webhook_path = os.getenv("N8N_WEBHOOK_DATA_FETCH", "/webhook/plantopia-data-fetch")
        return await self.call_webhook(webhook_path, {
            "dataType": data_type,
            "parameters": parameters,
            "timestamp": datetime.utcnow().isoformat()
        })
    
    async def health_check(self) -> bool:
        """
        Check if n8n is healthy and reachable
        
        Returns:
            True if n8n is healthy, False otherwise
        """
        try:
            async with httpx.AsyncClient() as client:
                response = await client.get(
                    f"{self.base_url}/healthz",
                    timeout=5.0
                )
                return response.status_code == 200
        except Exception:
            return False


# Singleton instance
n8n_client = N8NClient()
