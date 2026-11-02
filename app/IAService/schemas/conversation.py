from datetime import datetime
from uuid import UUID
from pydantic import BaseModel, ConfigDict, Field

class ConversationCreate(BaseModel):
    """Information required to register a conversation."""
    
    # Information about the conversation
    title: str = Field(min_length=1)
    
class ConversationUpdate(BaseModel):
    """Fields that can be modified after creating a conversation."""
    
    # IDs of the conversation and the user who created it
    id: UUID
    
    # Information about the conversation
    title: str | None = Field(default=None, min_length=1)
    is_archived: bool | None = None
    is_favorite: bool | None = None
    
class ConversationResponse(BaseModel):
    """Information returned when consulting a conversation."""
    
    # IDs of the conversation and the user who created it
    id: UUID
    user_id: UUID
    
    # Information about the conversation
    title: str
    is_archived: bool
    is_favorite: bool
    
    # Statistics for the conversation
    message_count: int
    total_tokens: int
    total_response_time: int
    average_response_time: float
    
    # Date and time information for the conversation
    created_at: datetime
    updated_at: datetime
    
    model_config = ConfigDict(from_attributes=True)
    
class ConversationListResponse(BaseModel):
    """Information returned when consulting a list of conversations."""
    
    # List of conversations
    conversations: list[ConversationResponse]
    
    model_config = ConfigDict(from_attributes=True)