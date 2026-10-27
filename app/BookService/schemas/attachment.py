from datetime import datetime
from uuid import UUID
from pydantic import BaseModel, ConfigDict, Field, HttpUrl

class AttachmentCreate(BaseModel):
    """Information required to register an attachment."""
    
    # ID of the book to which the attachment belongs
    book_id: UUID
    
    # Information about the attachment
    filename: str = Field(min_length=1, max_length=255)
    original_filename: str = Field(min_length=1, max_length=255)
    file_path: str = Field(min_length=1)
    file_type: str = Field(min_length=1, max_length=100)
    file_size_bytes: int = Field(ge=0)
    mime_type: str = Field(min_length=1, max_length=255)
    
    # Configuration for the attachment's visibility and access control
    is_favorite: bool = False
    is_public: bool = False

class AttachmentUpdate(BaseModel):
    """Fields that can be modified after creating an attachment."""
    
    # Information about the attachment
    filename: str | None = Field(default=None, min_length=1, max_length=255)
    original_filename: str | None = Field(default=None, min_length=1, max_length=255)
    file_type: str | None = Field(default=None, min_length=1, max_length=100)
    
    # Configuration for the attachment's visibility and access control
    is_public: bool | None = None
    is_favorite: bool | None = None

class AttachmentResponse(BaseModel):
    """Information returned when consulting an attachment."""
    
    # IDs of the attachment, the book it belongs to, and the user who created it
    id: UUID
    book_id: UUID
    user_id: UUID
    
    # Information about the attachment
    filename: str
    original_filename: str
    file_type: str
    file_size_bytes: int
    mime_type: str
    
    # Configuration for the attachment's visibility and access control
    is_public: bool
    is_favorite: bool
    view_count: int
    download_count: int
    
    # Date and time information for the attachment
    last_accessed_at: datetime | None
    created_at: datetime
    updated_at: datetime

    model_config = {"from_attributes": True}

class AttachmentFilter(BaseModel):
    """Filters available when listing attachments."""
    
    # IDs of the book to which the attachment belongs
    book_id: UUID | None = None
    
    # Information about the attachment
    file_type: str | None = Field(default=None, min_length=1, max_length=100)
    mime_type: str | None = Field(default=None, min_length=1, max_length=255)
    
    # Configuration for the attachment's visibility and access control
    is_public: bool | None = None
    is_favorite: bool | None = None
    
    # Search term for filtering attachments by filename or original filename
    search: str | None = Field(default=None, min_length=1, max_length=255)
    page: int = Field(default=1, ge=1)
    page_size: int = Field(default=20, ge=1, le=100)

class AttachmentListResponse(BaseModel):
    """Paginated list of attachments."""
    
    # List of attachments and pagination information
    items: list[AttachmentResponse]
    
    # Total number of attachments, current page, page size, and total pages
    total: int = Field(ge=0)
    page: int = Field(ge=1)
    page_size: int = Field(ge=1)
    pages: int = Field(ge=0)

class AttachmentLinkCreate(BaseModel):
    """Data required to register a URL instead of a physical file."""
    
    # ID of the book to which the attachment belongs
    book_id: UUID
    
    # Information about the attachment
    url: HttpUrl
    filename: str = Field(min_length=1, max_length=255)
    
    # Configuration for the attachment's visibility and access control
    is_public: bool = False