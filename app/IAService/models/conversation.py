from sqlalchemy import Integer, Float, Text, DateTime, ForeignKey, func, UUID
from sqlalchemy.orm import Mapped, mapped_column, relationship
from database.base import Base
from typing import TYPE_CHECKING
from datetime import datetime
import uuid

if TYPE_CHECKING:
    from .message import Message
    
class Conversation(Base):
    __tablename__ = "conversations"
    
    #IDs of the conversation and the user who created it
    id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    user_id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), nullable=False, index=True)
    
    # Information about the conversation
    title: Mapped[str] = mapped_column(Text(), nullable=False)
    
    # Configuration for the conversation's visibility and access control
    is_archived: Mapped[bool] = mapped_column(nullable=False, default=False)
    is_favorite: Mapped[bool] = mapped_column(nullable=False, default=False)
    
    # Statistics for the conversation
    message_count: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    total_tokens: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    total_response_time: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    average_response_time: Mapped[float] = mapped_column(Float(), nullable=False, default=0.0)
    
    # Date and time information for the conversation
    created_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now())
    updated_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now(), onupdate=func.now())
    
    # Relationship to the messages in the conversation
    messages: Mapped[list["Message"]] = relationship("Message", back_populates="conversation", cascade="all, delete-orphan")