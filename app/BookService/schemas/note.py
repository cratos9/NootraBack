from uuid import UUID
from datetime import datetime
from pydantic import BaseModel, Field

class NoteCreate(BaseModel):
    """Represents the data needed to create a new note."""
    
    # ID of the book to which the note belongs
    book_id: UUID
    
    # Basic information about the note
    title: str
    content: str | None = None
    
    # Configuration for the note's visibility and access control
    is_public: bool = False
    is_archived: bool = False
    is_favorite: bool = False
    
class NoteUpdate(BaseModel):
    """Represents the data needed to update an existing note."""
    
    # ID of the note to be updated
    book_id: UUID
    
    # Basic information about the note
    title: str | None = None
    content: str | None = None
    
    # Configuration for the note's visibility and access control
    is_public: bool | None = None
    is_archived: bool | None = None
    is_favorite: bool | None = None
    
class NoteResponse(BaseModel):
    """Represents the data returned when retrieving a note."""
    
    # ID of the note and its associated book
    id: UUID
    book_id: UUID
    user_id: UUID
    
    # Basic information about the note
    title: str
    content: str | None = None
    
    # Configuration for the note's visibility and access control
    is_public: bool
    is_archived: bool
    is_favorite: bool
    
    # Statistics for the note
    view_count: int
    word_count: int
    
    # Timestamps for creation and last update
    created_at: datetime
    updated_at: datetime
    
    model_config = {"from_attributes": True}
    
class NoteDeleteResponse(BaseModel):
    """Represents the data returned when deleting a note."""
    
    # ID of the deleted note
    id: UUID

class NotePublish(BaseModel):
    """Represents the data needed to publish a note."""
    
    # ID of the note to be published
    id: UUID

class NoteFavorite(BaseModel):
    """Represents the data needed to mark a note as favorite."""
    
    # ID of the note to be marked as favorite
    id: UUID

class NoteArchive(BaseModel):
    """Represents the data needed to archive a note."""
    
    # ID of the note to be archived
    id: UUID
    
class NotesResponse(BaseModel):
    """Represents the data returned when retrieving multiple notes."""
    
    # List of notes and pagination information
    notes: list[NoteResponse]
    
    # Information about pagination of notes
    page: int
    page_size: int
    total_items: int
    total_pages: int