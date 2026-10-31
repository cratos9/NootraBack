from sqlalchemy import Integer, Text, DateTime, ForeignKey, func, UUID, Boolean
from sqlalchemy.orm import Mapped, mapped_column, relationship
from database.base import Base
from typing import TYPE_CHECKING
from datetime import datetime
import uuid

if TYPE_CHECKING:
    from .conversation import Conversation
    
class Message(Base):
    __tablename__ = "messages"
    
    #IDs of the message and parent conversation
    id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    conversation_id: Mapped[uuid.UUID] = mapped_column(ForeignKey("conversations.id", ondelete="CASCADE"), nullable=False, index=True)
    
    # Information about the message
    order: Mapped[int] = mapped_column(Integer(), nullable=False)
    query_text: Mapped[str] = mapped_column(Text(), nullable=False)
    response_text: Mapped[str] = mapped_column(Text(), nullable=False)
    
    # Metadata for the message
    query_type: Mapped[str] = mapped_column(Text(), nullable=False)
    query_tokens: Mapped[int] = mapped_column(Integer(), nullable=False)
    response_time_ms: Mapped[int] = mapped_column(Integer(), nullable=False)
    response_tokens: Mapped[int] = mapped_column(Integer(), nullable=False)
    rating: Mapped[int] = mapped_column(Integer(), nullable=True)
    is_helpful: Mapped[bool] = mapped_column(Boolean(), nullable=True)
    language: Mapped[str] = mapped_column(Text(), nullable=False, default="es")
    subject: Mapped[str] = mapped_column(Text(), nullable=True)
    
    # Date and time information for the message
    created_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now())
    updated_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now(), onupdate=func.now())
    
    # Relationship to the parent conversation
    conversation: Mapped["Conversation"] = relationship("Conversation", back_populates="messages")