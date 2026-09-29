from uuid import UUID
from datetime import datetime
from pydantic import BaseModel

class BookCreate(BaseModel):
    """Represents the data needed to create a new book."""
    
    # ID of the parent book
    parent_id: UUID | None = None
    
    # Basic information about the book
    title: str
    description: str | None = None
    content: str | None = None
    color: str | None = None
    icon: str | None = None
    
    # Categorization and tagging for the book
    category: str
    semester: str | None = None
    tags: str | None = None
    
    # Configuration for the book's visibility and access control
    is_public: bool = False
    is_archived: bool = False
    is_favorite: bool = False
    
class BookUpdate(BaseModel):
    """Represents the data needed to update an existing book."""
    
    # ID of the parent book
    parent_id: UUID | None = None
    
    # Basic information about the book
    title: str | None = None
    description: str | None = None
    content: str | None = None
    color: str | None = None
    icon: str | None = None
    
    # Categorization and tagging for the book
    category: str | None = None
    semester: str | None = None
    tags: str | None = None
    
    # Configuration for the book's visibility and access control
    is_public: bool | None = None
    is_archived: bool | None = None
    is_favorite: bool | None = None
    
class BookResponse(BaseModel):
    """Represents the data returned when retrieving a book."""
    
    # ID of the book and its parent
    id: UUID
    parent_id: UUID | None = None
    user_id: UUID
    
    # Basic information about the book
    title: str
    description: str | None = None
    content: str | None = None
    color: str | None = None
    icon: str | None = None
    
    # Categorization and tagging for the book
    category: str
    semester: str | None = None
    tags: str | None = None
    
    # Configuration for the book's visibility and access control
    is_public: bool
    is_archived: bool
    is_favorite: bool
    order_count: int
    
    # Statistics about the book
    view_count: int
    file_count: int
    word_count: int
    note_count: int
    
    # Date and time information for the book
    last_accessed_at: datetime | None = None
    created_at: datetime
    updated_at: datetime
    
    model_config = {"from_attributes": True}
    
class BookDeleteResponse(BaseModel):
    """Represents the data returned when a book is deleted."""
    
    # ID of the deleted book
    id: UUID