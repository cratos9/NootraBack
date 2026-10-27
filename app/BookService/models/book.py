from sqlalchemy import Integer, Text, DateTime, ForeignKey, func, UUID
from sqlalchemy.orm import Mapped, mapped_column, relationship
from database.base import Base
from typing import TYPE_CHECKING
from datetime import datetime
import uuid

if TYPE_CHECKING:
    from .note import Note
    from .attachment import Attachment

class Book(Base):
    __tablename__ = "books"

    # IDs of the book, parent book (if any), and the user who created it
    id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    parent_id: Mapped[uuid.UUID | None] = mapped_column(ForeignKey("books.id", ondelete="CASCADE"), nullable=True, index=True)
    user_id: Mapped[uuid.UUID] = mapped_column(UUID(as_uuid=True), nullable=False, index=True)
    
    # Basic information about the book
    title: Mapped[str] = mapped_column(Text(), nullable=False)
    description: Mapped[str] = mapped_column(Text(), nullable=True)
    content: Mapped[str] = mapped_column(Text(), nullable=True)
    
    # Perzonalization for the book
    color: Mapped[str] = mapped_column(Text(), nullable=True) 
    icon: Mapped[str] = mapped_column(Text(), nullable=True)
    
    # Categorization and tagging for the book
    category: Mapped[str] = mapped_column(Text(), nullable=False, index=True)
    semester: Mapped[str] = mapped_column(Text(), nullable=True, index=True)
    tags: Mapped[str] = mapped_column(Text(), nullable=True, index=True)
    
    # Configuration for the book's visibility and access control
    is_public: Mapped[bool] = mapped_column(nullable=False, default=False)
    is_archived: Mapped[bool] = mapped_column(nullable=False, default=False)
    is_favorite: Mapped[bool] = mapped_column(nullable=False, default=False)
    order_index: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    
    # Stadistics
    view_count: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    file_count: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    word_count: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    note_count: Mapped[int] = mapped_column(Integer(), nullable=False, default=0)
    
    # Date and time information for the book
    last_accessed_at: Mapped[datetime] = mapped_column(DateTime(), nullable=True)
    created_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now())
    updated_at: Mapped[datetime] = mapped_column(DateTime(), nullable=False, server_default=func.now(), onupdate=func.now())
    
    # Relationships with other tables (if any)
    children: Mapped[list["Book"]] = relationship("Book", back_populates="parent", cascade="all, delete-orphan")
    parent: Mapped["Book | None"] = relationship("Book", back_populates="children", remote_side=[id])
    notes: Mapped[list["Note"]] = relationship("Note", back_populates="book", cascade="all, delete-orphan")
    attachments: Mapped[list["Attachment"]] = relationship("Attachment", back_populates="book", cascade="all, delete-orphan")