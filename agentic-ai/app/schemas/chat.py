from pydantic import BaseModel, Field
from typing import Optional, List, Dict, Any

class ChatMessage(BaseModel):
    role: str = Field(..., description="Role of the sender: user, assistant, system")
    content: str = Field(..., description="Content of the message")

class ChatRequest(BaseModel):
    messages: List[ChatMessage] = Field(..., description="Conversation history")
    company_id: str = Field(..., description="Company ID for isolation")

class ChatResponse(BaseModel):
    response: str = Field(..., description="The assistant's reply")
