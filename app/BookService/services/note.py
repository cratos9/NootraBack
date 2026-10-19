from uuid import UUID
from sqlalchemy.orm import Session
from models.note import Note
from repositories import note as note_repo
from math import ceil
from redis.exceptions import RedisError
from cryptography.fernet import InvalidToken
from database.redis import redis_client
from core.config import settings
from core.security import encrypt_data, decrypt_data
from schemas.note import NoteCreate, NoteUpdate, NoteResponse, NotesResponse
import logging

logger = logging.getLogger(__name__)
SENSITIVE_FIELDS = ("title", "content")


def invalidate_note_cache(note_id: UUID, user_id: UUID):
    """Invalidates the cache for a specific note."""
    cache_key = f"note:{note_id}:{user_id}"
    try:
        redis_client.delete(cache_key)
        logger.info("Cache invalidated for note ID: %s | User ID: %s", note_id, user_id)
    except RedisError as e:
        logger.error("Redis error while invalidating cache for note ID: %s | User ID: %s | Error: %s", note_id, user_id, e)


def _encrypt_data(value: str | None) -> str | None:
    """Encrypts sensitive fields of a note."""
    return encrypt_data(value) if value else value


def _decrypt_note_data(value: str | None) -> str | None:
    """Decrypts sensitive fields of a note."""
    return decrypt_data(value) if value else value


def _encrypt_note_fields(note: Note, fields=SENSITIVE_FIELDS) -> Note:
    """Encrypts selected sensitive fields before saving."""
    for field in fields:
        value = getattr(note, field, None)
        if value:
            setattr(note, field, _encrypt_data(value))
    return note


def _note_response(note: Note | NoteResponse) -> NoteResponse:
    """Returns a note with its sensitive fields decrypted."""
    response = NoteResponse.model_validate(note)
    return response.model_copy(update={
        field: _decrypt_note_data(getattr(response, field))
        for field in SENSITIVE_FIELDS
        if getattr(response, field, None)
    })


def create_note(db: Session, note_data: NoteCreate, user_id: UUID) -> NoteResponse:
    """Creates a new note in the database."""
    try:
        note = note_repo.create(db=db, note_data=note_data, user_id=user_id)
        _encrypt_note_fields(note)
        db.commit()
        db.refresh(note)
        logger.info("Note created successfully | Note ID: %s | User ID: %s", note.id, user_id)
        return _note_response(note)
    except Exception:
        db.rollback()
        logger.error("Error creating note | User ID: %s", user_id, exc_info=True)
        raise


def get_note_by_id(db: Session, book_id: UUID, note_id: UUID, user_id: UUID) -> NoteResponse | None:
    """Retrieves a note by its ID, book ID, and user ID."""
    cache_key = f"note:{note_id}:{user_id}"
    try:
        cached_note = redis_client.get(cache_key)
        if cached_note:
            try:
                logger.info("Cache hit for note ID: %s | User ID: %s", note_id, user_id)
                return _note_response(NoteResponse.model_validate_json(cached_note))
            except (InvalidToken, ValueError, TypeError):
                logger.warning("Invalid cached note data | Note ID: %s | User ID: %s", note_id, user_id, exc_info=True)
                try:
                    redis_client.delete(cache_key)
                except RedisError:
                    logger.error("Redis error removing invalid note cache | Note ID: %s | User ID: %s", note_id, user_id, exc_info=True)
    except RedisError as e:
        logger.error("Redis error while fetching note ID: %s | User ID: %s | Error: %s", note_id, user_id, e)

    response = note_repo.get_by_id(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
    if response:
        try:
            redis_client.setex(cache_key, settings.redis_cache_expire, NoteResponse.model_validate(response).model_dump_json())
        except RedisError as e:
            logger.error("Redis error while caching note ID: %s | User ID: %s | Error: %s", note_id, user_id, e)
        return _note_response(response)
    return None


def update_note(db: Session, book_id: UUID, note_id: UUID, note_data: NoteUpdate, user_id: UUID) -> NoteResponse | None:
    """Updates an existing note in the database."""
    try:
        note = note_repo.get_by_id(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
        if note:
            fields_to_encrypt = [field for field in SENSITIVE_FIELDS if field in note_data.model_fields_set]
            updated_note = note_repo.update(db=db, note=note, note_data=note_data)
            _encrypt_note_fields(updated_note, fields_to_encrypt)
            db.commit()
            db.refresh(updated_note)
            invalidate_note_cache(note_id=note_id, user_id=user_id)
            logger.info("Note updated successfully | Note ID: %s | User ID: %s", note_id, user_id)
            return _note_response(updated_note)
        logger.warning("Note not found for update | Note ID: %s | User ID: %s", note_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error updating note | Note ID: %s | User ID: %s", note_id, user_id, exc_info=True)
        raise


def delete_note(db: Session, book_id: UUID, note_id: UUID, user_id: UUID) -> bool:
    """Deletes a note from the database."""
    try:
        note = note_repo.get_by_id(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
        if note:
            note_repo.delete(db=db, note=note)
            db.commit()
            invalidate_note_cache(note_id=note_id, user_id=user_id)
            logger.info("Note deleted successfully | Note ID: %s | User ID: %s", note_id, user_id)
            return True
        logger.warning("Note not found for deletion | Note ID: %s | User ID: %s", note_id, user_id)
        return False
    except Exception:
        db.rollback()
        logger.error("Error deleting note | Note ID: %s | User ID: %s", note_id, user_id, exc_info=True)
        raise


def toggle_favorite(db: Session, book_id: UUID, note_id: UUID, user_id: UUID) -> NoteResponse | None:
    """Toggles the favorite status of a note."""
    try:
        note = note_repo.favorite(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
        if note:
            db.commit()
            db.refresh(note)
            invalidate_note_cache(note_id=note_id, user_id=user_id)
            logger.info("Note favorite status toggled | Note ID: %s | User ID: %s", note_id, user_id)
            return _note_response(note)
        logger.warning("Note not found for toggling favorite | Note ID: %s | User ID: %s", note_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling favorite status for note | Note ID: %s | User ID: %s", note_id, user_id, exc_info=True)
        raise


def toggle_archive(db: Session, book_id: UUID, note_id: UUID, user_id: UUID) -> NoteResponse | None:
    """Toggles the archive status of a note."""
    try:
        note = note_repo.archive(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
        if note:
            db.commit()
            db.refresh(note)
            invalidate_note_cache(note_id=note_id, user_id=user_id)
            logger.info("Note archive status toggled | Note ID: %s | User ID: %s", note_id, user_id)
            return _note_response(note)
        logger.warning("Note not found for toggling archive | Note ID: %s | User ID: %s", note_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling archive status for note | Note ID: %s | User ID: %s", note_id, user_id, exc_info=True)
        raise


def toggle_public(db: Session, book_id: UUID, note_id: UUID, user_id: UUID) -> NoteResponse | None:
    """Toggles the public status of a note."""
    try:
        note = note_repo.public(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
        if note:
            db.commit()
            db.refresh(note)
            invalidate_note_cache(note_id=note_id, user_id=user_id)
            logger.info("Note public status toggled | Note ID: %s | User ID: %s", note_id, user_id)
            return _note_response(note)
        logger.warning("Note not found for toggling public | Note ID: %s | User ID: %s", note_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling public status for note | Note ID: %s | User ID: %s", note_id, user_id, exc_info=True)
        raise


def get_public_notes(db: Session, book_id: UUID, page: int, page_size: int) -> NotesResponse:
    """Retrieves public notes for a specific book with pagination."""
    try:
        notes, total_items = note_repo.get_public_notes(db=db, book_id=book_id, page=page, page_size=page_size)
        total_pages = ceil(total_items / page_size) if page_size else 0
        logger.info("Public notes retrieved successfully | Book ID: %s | Page: %d | Page Size: %d", book_id, page, page_size)
        return NotesResponse(notes=[_note_response(note) for note in notes], page=page, page_size=page_size, total_items=total_items, total_pages=total_pages)
    except Exception:
        logger.error("Error retrieving public notes | Book ID: %s | Page: %d | Page Size: %d", book_id, page, page_size, exc_info=True)
        raise


def _build_notes_response(notes: list[Note], page: int, page_size: int, total_items: int) -> NotesResponse:
    """Builds a NotesResponse object from the given notes and pagination info."""
    try:
        total_pages = ceil(total_items / page_size) if page_size else 0
        logger.info("Notes response built successfully | Page: %d | Page Size: %d | Total Items: %d", page, page_size, total_items)
        return NotesResponse(notes=[_note_response(note) for note in notes], page=page, page_size=page_size, total_items=total_items, total_pages=total_pages)
    except Exception:
        logger.error("Error building notes response | Page: %d | Page Size: %d | Total Items: %d", page, page_size, total_items, exc_info=True)
        raise


def get_active_notes(db: Session, user_id: UUID, book_id: UUID, page: int, page_size: int) -> NotesResponse:
    """Retrieves active notes for a specific book and user with pagination."""
    try:
        notes, total_items = note_repo.get_active_notes(db=db, user_id=user_id, book_id=book_id, page=page, page_size=page_size)
        logger.info("Active notes retrieved successfully | Book ID: %s | User ID: %s | Page: %d | Page Size: %d", book_id, user_id, page, page_size)
        return _build_notes_response(notes=notes, page=page, page_size=page_size, total_items=total_items)
    except Exception:
        logger.error("Error retrieving active notes | Book ID: %s | User ID: %s | Page: %d | Page Size: %d", book_id, user_id, page, page_size, exc_info=True)
        raise


def get_archived_notes(db: Session, user_id: UUID, book_id: UUID, page: int, page_size: int) -> NotesResponse:
    """Retrieves archived notes for a specific book and user with pagination."""
    try:
        notes, total_items = note_repo.get_archived_notes(db=db, user_id=user_id, book_id=book_id, page=page, page_size=page_size)
        logger.info("Archived notes retrieved successfully | Book ID: %s | User ID: %s | Page: %d | Page Size: %d", book_id, user_id, page, page_size)
        return _build_notes_response(notes=notes, page=page, page_size=page_size, total_items=total_items)
    except Exception:
        logger.error("Error retrieving archived notes | Book ID: %s | User ID: %s | Page: %d | Page Size: %d", book_id, user_id, page, page_size, exc_info=True)
        raise
