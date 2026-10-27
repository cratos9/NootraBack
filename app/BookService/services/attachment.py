from uuid import UUID, uuid4
from pathlib import Path
from math import ceil
import logging
import mimetypes
from fastapi import UploadFile
from sqlalchemy.orm import Session
from redis.exceptions import RedisError
from database.redis import redis_client
from core.config import settings
from models.attachment import Attachment
from repositories import attachment as attachment_repo
from repositories import book as book_repo
from schemas.attachment import AttachmentResponse, AttachmentUpdate, AttachmentListResponse


logger = logging.getLogger(__name__)

STORAGE_DIR = Path(__file__).resolve().parents[1] / "storage" / "attachments"
MAX_FILE_SIZE = 50 * 1024 * 1024

ALLOWED_EXTENSIONS = {
    ".pdf", ".doc", ".docx", ".txt",
    ".png", ".jpg", ".jpeg", ".webp", ".gif",
    ".mp4", ".webm", ".mov",
    ".ppt", ".pptx", ".xls", ".xlsx",
    ".csv", ".zip"
}

def invalidate_attachment_cache(attachment_id: UUID, user_id: UUID):
    """Invalidates the cache for a specific attachment."""

    cache_key = f"attachment:{attachment_id}:{user_id}"
    try:
        redis_client.delete(cache_key)
        logger.info("Attachment cache invalidated | Attachment ID: %s | User ID: %s", attachment_id, user_id)
    except RedisError:
        logger.error("Redis error invalidating attachment cache | Attachment ID: %s | User ID: %s", attachment_id, user_id, exc_info=True)

def _attachment_response(attachment: Attachment | AttachmentResponse) -> AttachmentResponse:
    """Builds an attachment response from an ORM object."""

    return AttachmentResponse.model_validate(attachment)

def _get_file_type(filename: str) -> str:
    """Determines the logical category of an attachment."""

    extension = Path(filename).suffix.lower()
    if extension in {".pdf", ".doc", ".docx", ".txt", ".ppt", ".pptx", ".xls", ".xlsx", ".csv"}:
        return "document"

    if extension in {".png", ".jpg", ".jpeg", ".webp", ".gif"}:
        return "image"

    if extension in {".mp4", ".webm", ".mov"}:
        return "video"

    if extension == ".zip":
        return "other"

    return "other"

def create_attachment(db: Session, book_id: UUID, user_id: UUID, file: UploadFile, is_public: bool = False) -> AttachmentResponse:
    """Uploads a file and registers its metadata in the database."""

    file_path = None
    try:
        book = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)

        if not book:
            raise ValueError("Book not found or access denied.")

        original_filename = Path(file.filename or "").name
        if not original_filename:
            raise ValueError("The uploaded file must have a filename.")

        extension = Path(original_filename).suffix.lower()
        if extension not in ALLOWED_EXTENSIONS:
            raise ValueError("File extension is not allowed.")

        STORAGE_DIR.mkdir(parents=True, exist_ok=True)
        stored_filename = f"{uuid4().hex}{extension}"
        file_path = STORAGE_DIR / stored_filename
        file_size = 0

        with file_path.open("wb") as destination:
            while chunk := file.file.read(1024 * 1024):
                file_size += len(chunk)
                if file_size > MAX_FILE_SIZE:
                    raise ValueError("File exceeds the 50 MB limit.")
                destination.write(chunk)

        if file_size == 0:
            raise ValueError("Empty files are not allowed.")

        mime_type = (mimetypes.guess_type(original_filename)[0] or "application/octet-stream")

        attachment = Attachment(
            book_id=book_id,
            user_id=user_id,
            filename=stored_filename,
            original_filename=original_filename,
            file_path=str(file_path),
            file_type=_get_file_type(original_filename),
            file_size_bytes=file_size,
            mime_type=mime_type,
            is_public=is_public
        )

        attachment_repo.create(db=db, attachment=attachment)
        db.commit()
        db.refresh(attachment)
        logger.info("Attachment created | Attachment ID: %s | Book ID: %s | User ID: %s", attachment.id, book_id, user_id)

        return _attachment_response(attachment)

    except Exception:
        db.rollback()

        if file_path and file_path.exists():
            try:
                file_path.unlink()
            except OSError:
                logger.error("Could not remove incomplete upload | Path: %s", file_path, exc_info=True)
        logger.error("Error creating attachment | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise

def get_attachment_by_id(db: Session, attachment_id: UUID, user_id: UUID) -> AttachmentResponse | None:
    """Retrieves an attachment by ID and owner, using Redis cache."""

    cache_key = f"attachment:{attachment_id}:{user_id}"
    try:
        cached_attachment = redis_client.get(cache_key)

        if cached_attachment:
            try:
                logger.info("Attachment cache hit | Attachment ID: %s | User ID: %s", attachment_id, user_id)
                return AttachmentResponse.model_validate_json(cached_attachment)
            except (ValueError, TypeError):
                logger.warning("Invalid cached attachment | Attachment ID: %s", attachment_id, exc_info=True)
                try:
                    redis_client.delete(cache_key)
                except RedisError:
                    logger.error("Redis error removing invalid attachment cache", exc_info=True)
    except RedisError:
        logger.error("Redis error fetching attachment cache | Attachment ID: %s", attachment_id, exc_info=True)

    attachment = attachment_repo.get_by_id(db=db, attachment_id=attachment_id, user_id=user_id)
    if not attachment:
        return None

    response = _attachment_response(attachment)
    try:
        redis_client.setex(cache_key, settings.redis_cache_expire, response.model_dump_json())
    except RedisError:
        logger.error("Redis error caching attachment | Attachment ID: %s", attachment_id, exc_info=True)

    return response


def update_attachment(db: Session, attachment_id: UUID, attachment_data: AttachmentUpdate, user_id: UUID) -> AttachmentResponse | None:
    """Updates an attachment's metadata."""

    try:
        attachment = attachment_repo.get_by_id(db=db, attachment_id=attachment_id, user_id=user_id)
        if not attachment:
            logger.warning("Attachment not found for update | Attachment ID: %s", attachment_id)
            return None
        attachment_repo.update(db=db, attachment=attachment, attachment_data=attachment_data)

        db.commit()
        db.refresh(attachment)

        invalidate_attachment_cache(attachment_id=attachment_id, user_id=user_id)
        logger.info("Attachment updated | Attachment ID: %s | User ID: %s", attachment_id, user_id)

        return _attachment_response(attachment)

    except Exception:
        db.rollback()
        logger.error("Error updating attachment | Attachment ID: %s | User ID: %s", attachment_id, user_id, exc_info=True)
        raise


def delete_attachment(db: Session, attachment_id: UUID, user_id: UUID) -> bool:
    """Deletes an attachment record and its physical file."""

    try:
        attachment = attachment_repo.get_by_id(db=db, attachment_id=attachment_id, user_id=user_id)
        if not attachment:
            logger.warning("Attachment not found for deletion | Attachment ID: %s", attachment_id)
            return False

        file_path = Path(attachment.file_path)
        attachment_repo.delete(db=db, attachment=attachment)
        db.commit()

        invalidate_attachment_cache(attachment_id=attachment_id, user_id=user_id)
        try:
            file_path.unlink(missing_ok=True)
        except OSError:
            logger.error("Database record deleted but physical file cleanup failed | Path: %s", file_path, exc_info=True)
        logger.info("Attachment deleted | Attachment ID: %s | User ID: %s", attachment_id, user_id)

        return True

    except Exception:
        db.rollback()
        logger.error("Error deleting attachment | Attachment ID: %s | User ID: %s", attachment_id, user_id, exc_info=True)
        raise


def toggle_favorite(db: Session, attachment_id: UUID, user_id: UUID) -> AttachmentResponse | None:
    """Toggles an attachment's favorite status."""

    try:
        attachment = attachment_repo.favorite(db=db, attachment_id=attachment_id, user_id=user_id)
        if not attachment:
            logger.warning("Attachment not found for favorite toggle | Attachment ID: %s", attachment_id)
            return None

        db.commit()
        db.refresh(attachment)

        invalidate_attachment_cache(attachment_id, user_id)
        return _attachment_response(attachment)

    except Exception:
        db.rollback()
        logger.error("Error toggling attachment favorite | Attachment ID: %s", attachment_id, exc_info=True)
        raise


def toggle_public(db: Session, attachment_id: UUID, user_id: UUID) -> AttachmentResponse | None:
    """Toggles an attachment's public visibility."""

    try:
        attachment = attachment_repo.public(db=db, attachment_id=attachment_id, user_id=user_id)
        if not attachment:
            logger.warning("Attachment not found for public toggle | Attachment ID: %s", attachment_id)
            return None

        db.commit()
        db.refresh(attachment)

        invalidate_attachment_cache(attachment_id, user_id)
        return _attachment_response(attachment)

    except Exception:
        db.rollback()
        logger.error("Error toggling attachment visibility | Attachment ID: %s", attachment_id, exc_info=True)
        raise


def _build_attachment_list(attachments: list[Attachment], page: int, page_size: int, total_items: int) -> AttachmentListResponse:
    """Builds a paginated attachment response."""

    total_pages = ceil(total_items / page_size) if total_items else 0
    return AttachmentListResponse(
        items=[_attachment_response(attachment) for attachment in attachments],
        total=total_items,
        page=page,
        page_size=page_size,
        pages=total_pages
    )


def get_attachments(db: Session, user_id: UUID, page: int = 1, page_size: int = 20, book_id: UUID | None = None, file_type: str | None = None, mime_type: str | None = None, is_public: bool | None = None, is_favorite: bool | None = None, search: str | None = None) -> AttachmentListResponse:
    """Retrieves a user's attachments with filters and pagination."""

    try:
        attachments, total_items = attachment_repo.get_all(db=db, user_id=user_id, page=page, page_size=page_size, book_id=book_id, file_type=file_type, mime_type=mime_type, is_public=is_public, is_favorite=is_favorite, search=search)
        return _build_attachment_list(attachments, page, page_size, total_items)
    except Exception:
        logger.error("Error retrieving attachments | User ID: %s", user_id, exc_info=True)
        raise


def get_public_attachments(db: Session, page: int = 1, page_size: int = 20, file_type: str | None = None, mime_type: str | None = None, search: str | None = None) -> AttachmentListResponse:
    """Retrieves public attachments with filters and pagination."""

    try:
        attachments, total_items = attachment_repo.get_public_attachments(db=db, page=page, page_size=page_size, file_type=file_type, mime_type=mime_type, search=search)
        return _build_attachment_list(attachments, page, page_size, total_items)
    except Exception:
        logger.error("Error retrieving public attachments | Page: %d | Page Size: %d", page, page_size, exc_info=True)
        raise
