from sqlalchemy import Integer, Text, DateTime, ForeignKey, func, UUID
from sqlalchemy.orm import Mapped, mapped_column, relationship
from database.base import Base
from typing import TYPE_CHECKING
from datetime import datetime
import uuid

if TYPE_CHECKING:
    from .book import Book
    
class Attachment(Base):
    __tablename__ = "attachments"
    
    #IDs of the atachment, parent book (if any), and the user who created it
    id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    book_id: Mapped[uuid.UUID] = mapped_column(ForeignKey("books.id", ondelete="CASCADE"), nullable=False, index=True)
    user_id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), nullable=False, index=True)
    
    # Information about the attachment
    filename: Mapped[str] = mapped_column(Text(), nullable=False)
    original_filename: Mapped[str] = mapped_column(Text(), nullable=False)
    file_path: Mapped[str] = mapped_column(Text(), nullable=False)
    
    # Metadata for the atachment
    file_type: Mapped[str] = mapped_column(Text(), nullable=False)
    file_size_bytes: Mapped[int] = mapped_column(Integer(), nullable=False)
    mime_type: Mapped[str] = mapped_column(Text(), nullable=False)
    
    # Configuration for the attachment's visibility and access control
    is_public: Mapped[bool] = mapped_column(nullable=False, default=False)
    is_favorite: Mapped[bool] = mapped_column(nullable=False, default=False)
    
    # Statistics for the attachment
    view_count: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    download_count: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    
    # Date and time information for the attachment
    last_accessed_at: Mapped[datetime] = mapped_column(DateTime(), nullable=True)
    created_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now())
    updated_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now(), onupdate=func.now())

    book: Mapped["Book"] = relationship("Book", back_populates="attachments")