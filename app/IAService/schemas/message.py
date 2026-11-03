from datetime import datetime
from uuid import UUID
from pydantic import BaseModel, ConfigDict, Field

class MessageCreate(BaseModel):
    """Information required to register a message."""
    
    # IDs of the conversation to which the message belongs
    conversation_id: UUID
    
    # Information about the message
    query_text: str = Field(min_length=1)
    
class MessageUpdate(BaseModel):
    """Fields that can be modified after creating a message."""
    
    # IDs of the message and the conversation to which it belongs
    id: UUID
    conversation_id: UUID
    
    # Information about the message
    query_text: str | None = Field(default=None, min_length=1)
    rating: int | None = Field(default=None, ge=1, le=5)
    is_helpful: bool | None = None
    subject: str | None = Field(default=None, min_length=1)
    
class MessageResponse(BaseModel):
    """Information returned when consulting a message."""
    
    # IDs of the message and the conversation to which it belongs
    id: UUID
    conversation_id: UUID
    
    # Information about the message
    order: int
    query_text: str
    response_text: str
    query_type: str
    query_tokens: int
    response_time_ms: int
    response_tokens: int
    rating: int | None
    is_helpful: bool | None
    
    # Date and time information for the message
    created_at: datetime
    updated_at: datetime
    
    model_config = ConfigDict(from_attributes=True)
    
class MessageListResponse(BaseModel):
    """Information returned when consulting a list of messages."""
    
    # List of messages
    messages: list[MessageResponse]
    
    model_config = ConfigDict(from_attributes=True)