from uuid import UUID
import logging
from fastapi import APIRouter, Depends, HTTPException, Request, Query, UploadFile, File, Form, status
from sqlalchemy.orm import Session
from database.session import get_db
from schemas.attachment import AttachmentResponse, AttachmentUpdate, AttachmentListResponse
from services import attachment as attachment_service
from api.dependencies.auth import get_current_user
from core.rate_limit import limiter


logger = logging.getLogger(__name__)

router = APIRouter(prefix="/attachments", tags=["Attachments"])


@router.post("", response_model=AttachmentResponse, status_code=status.HTTP_201_CREATED, summary="Upload a new attachment", description="Uploads a file and associates it with an existing book.")
@limiter.limit("10/minute")
def create_attachment(request: Request, book_id: UUID = Form(...), is_public: bool = Form(False), file: UploadFile = File(...), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Uploads a file and registers its metadata."""

    try:
        attachment = attachment_service.create_attachment(db=db, book_id=book_id, user_id=current_user, file=file, is_public=is_public)
        logger.info("Attachment uploaded | Attachment ID: %s | User ID: %s", attachment.id, current_user)
        return attachment
    except ValueError as e:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(e))
    finally:
        file.file.close()


@router.get("/public", response_model=AttachmentListResponse, summary="Get public attachments", description="Retrieves public attachments with optional filters and pagination.")
@limiter.limit("30/minute")
def get_public_attachments(request: Request, page: int = Query(1, ge=1), page_size: int = Query(20, ge=1, le=100), file_type: str | None = Query(None), mime_type: str | None = Query(None), search: str | None = Query(None, min_length=1, max_length=255), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves public attachments."""

    attachments = attachment_service.get_public_attachments(db=db, page=page, page_size=page_size, file_type=file_type, mime_type=mime_type, search=search)
    logger.info("Public attachments retrieved | Page: %d | Count: %d", page, len(attachments.items))

    return attachments


@router.get("", response_model=AttachmentListResponse, summary="Get user's attachments", description="Retrieves the current user's attachments with filters and pagination.")
@limiter.limit("30/minute")
def get_attachments(request: Request, page: int = Query(1, ge=1), page_size: int = Query(20, ge=1, le=100), book_id: UUID | None = Query(None), file_type: str | None = Query(None), mime_type: str | None = Query(None), is_public: bool | None = Query(None), is_favorite: bool | None = Query(None), search: str | None = Query(None, min_length=1, max_length=255), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves attachments belonging to the current user."""

    attachments = attachment_service.get_attachments(db=db, user_id=current_user, page=page, page_size=page_size, book_id=book_id, file_type=file_type, mime_type=mime_type, is_public=is_public, is_favorite=is_favorite, search=search)
    logger.info("Attachments retrieved | User ID: %s | Page: %d | Count: %d", current_user, page, len(attachments.items))
    return attachments


@router.get("/{attachment_id}", response_model=AttachmentResponse, summary="Get an attachment by ID", description="Retrieves an attachment owned by the current user.")
@limiter.limit("50/minute")
def get_attachment(request: Request, attachment_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves an attachment by its ID."""

    attachment = attachment_service.get_attachment_by_id(db=db, attachment_id=attachment_id, user_id=current_user)
    if not attachment:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Attachment not found")
    logger.info("Attachment retrieved | Attachment ID: %s | User ID: %s", attachment_id, current_user)

    return attachment


@router.put("/{attachment_id}", response_model=AttachmentResponse, summary="Update an attachment", description="Updates an attachment's metadata and visibility.")
@limiter.limit("20/minute")
def update_attachment(request: Request, attachment_id: UUID, attachment_data: AttachmentUpdate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Updates an existing attachment."""

    attachment = attachment_service.update_attachment(db=db, attachment_id=attachment_id, attachment_data=attachment_data, user_id=current_user)
    if not attachment:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Attachment not found")
    logger.info("Attachment updated | Attachment ID: %s | User ID: %s", attachment_id, current_user)
    return attachment


@router.delete("/{attachment_id}", response_model=bool, summary="Delete an attachment", description="Deletes an attachment and attempts to remove its physical file.")
@limiter.limit("20/minute")
def delete_attachment(request: Request, attachment_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Deletes an attachment."""

    success = attachment_service.delete_attachment(db=db, attachment_id=attachment_id, user_id=current_user)
    if not success:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Attachment not found")
    logger.info("Attachment deleted | Attachment ID: %s | User ID: %s", attachment_id, current_user)

    return success


@router.post("/{attachment_id}/favorite", response_model=AttachmentResponse, summary="Toggle attachment favorite", description="Marks an attachment as favorite or removes it from favorites.")
@limiter.limit("20/minute")
def favorite_attachment(request: Request, attachment_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Toggles an attachment's favorite status."""

    attachment = attachment_service.toggle_favorite(db=db, attachment_id=attachment_id, user_id=current_user)
    if not attachment:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Attachment not found")
    logger.info("Attachment favorite toggled | Attachment ID: %s | User ID: %s", attachment_id, current_user)

    return attachment


@router.post("/{attachment_id}/public", response_model=AttachmentResponse, summary="Toggle attachment public visibility", description="Makes an attachment public or private.")
@limiter.limit("20/minute")
def publish_attachment(request: Request, attachment_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Toggles an attachment's public visibility."""

    attachment = attachment_service.toggle_public(db=db, attachment_id=attachment_id, user_id=current_user)
    if not attachment:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Attachment not found")
    logger.info("Attachment visibility toggled | Attachment ID: %s | User ID: %s", attachment_id, current_user)

    return attachment
