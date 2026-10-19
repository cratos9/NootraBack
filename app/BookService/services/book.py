from uuid import UUID
from sqlalchemy.orm import Session
from models.book import Book
from repositories import book as book_repo
from math import ceil
from redis.exceptions import RedisError
from cryptography.fernet import InvalidToken
import json
from database.redis import redis_client
from core.config import settings
from core.security import encrypt_data, decrypt_data
from schemas.book import BookCreate, BookUpdate, BookResponse, BooksResponse
import logging

logger = logging.getLogger(__name__)
SENSITIVE_FIELDS = ("title", "description", "content", "category", "semester", "tags")


def invalidate_book_cache(book_id: UUID, user_id: UUID):
    """Invalidates the cache for a specific book."""
    cache_key = f"book:{book_id}:{user_id}"
    try:
        redis_client.delete(cache_key)
        logger.info("Cache invalidated for book ID: %s | User ID: %s", book_id, user_id)
    except RedisError as e:
        logger.error("Redis error while invalidating cache for book ID: %s | User ID: %s | Error: %s", book_id, user_id, e)


def _encrypt_data(value: str | None) -> str | None:
    """Encrypts sensitive fields of a book."""
    return encrypt_data(value) if value else value


def _decrypt_book_data(value: str | None) -> str | None:
    """Decrypts sensitive fields of a book."""
    return decrypt_data(value) if value else value


def _encrypt_book_fields(book: Book, fields=SENSITIVE_FIELDS) -> Book:
    """Encrypts selected sensitive fields before saving."""
    for field in fields:
        value = getattr(book, field, None)
        if value:
            setattr(book, field, _encrypt_data(value))
    return book


def _book_response(book: Book | BookResponse) -> BookResponse:
    """Returns a book with its sensitive fields decrypted."""
    response = BookResponse.model_validate(book)
    return response.model_copy(update={
        field: _decrypt_book_data(getattr(response, field))
        for field in SENSITIVE_FIELDS
        if getattr(response, field, None)
    })


def create_book(db: Session, book_data: BookCreate, user_id: UUID) -> BookResponse:
    """Creates a new book in the database."""
    try:
        book = book_repo.create(db=db, book_data=book_data, user_id=user_id)
        _encrypt_book_fields(book)
        db.commit()
        db.refresh(book)
        logger.info("Book created successfully | Book ID: %s | User ID: %s", book.id, user_id)
        return _book_response(book)
    except Exception:
        db.rollback()
        logger.error("Error creating book | User ID: %s", user_id, exc_info=True)
        raise


def get_book_by_id(db: Session, book_id: UUID, user_id: UUID) -> BookResponse | None:
    """Retrieves a book by its ID and user ID."""
    cache_key = f"book:{book_id}:{user_id}"
    try:
        cached_book = redis_client.get(cache_key)
        if cached_book:
            try:
                logger.info("Cache hit for book ID: %s | User ID: %s", book_id, user_id)
                return _book_response(BookResponse.model_validate_json(cached_book))
            except (InvalidToken, ValueError, TypeError):
                logger.warning("Invalid cached book data | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
                try:
                    redis_client.delete(cache_key)
                except RedisError:
                    logger.error("Redis error removing invalid cache | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
    except RedisError as e:
        logger.error("Redis error while fetching book ID: %s | User ID: %s | Error: %s", book_id, user_id, e)

    response = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)
    if response:
        try:
            redis_client.setex(cache_key, settings.redis_cache_expire, BookResponse.model_validate(response).model_dump_json())
        except RedisError as e:
            logger.error("Redis error while caching book ID: %s | User ID: %s | Error: %s", book_id, user_id, e)
        return _book_response(response)
    return None


def update_book(db: Session, book_id: UUID, book_data: BookUpdate, user_id: UUID) -> BookResponse | None:
    """Updates an existing book in the database."""
    try:
        book = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)
        if book:
            fields_to_encrypt = [field for field in SENSITIVE_FIELDS if field in book_data.model_fields_set]
            updated_book = book_repo.update(db=db, book=book, book_data=book_data)
            _encrypt_book_fields(updated_book, fields_to_encrypt)
            db.commit()
            db.refresh(updated_book)
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            logger.info("Book updated successfully | Book ID: %s | User ID: %s", book_id, user_id)
            return _book_response(updated_book)
        logger.warning("Book not found for update | Book ID: %s | User ID: %s", book_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error updating book | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise


def delete_book(db: Session, book_id: UUID, user_id: UUID) -> bool:
    """Deletes a book from the database."""
    try:
        book = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)
        if book:
            book_repo.delete(db=db, book=book)
            db.commit()
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            logger.info("Book deleted successfully | Book ID: %s | User ID: %s", book_id, user_id)
            return True
        logger.warning("Book not found for deletion | Book ID: %s | User ID: %s", book_id, user_id)
        return False
    except Exception:
        db.rollback()
        logger.error("Error deleting book | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise


def toggle_favorite(db: Session, book_id: UUID, user_id: UUID) -> BookResponse | None:
    """Toggles the favorite status of a book."""
    try:
        book = book_repo.favorite(db=db, book_id=book_id, user_id=user_id)
        if book:
            db.commit()
            db.refresh(book)
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            logger.info("Book favorite status toggled | Book ID: %s | User ID: %s", book_id, user_id)
            return _book_response(book)
        logger.warning("Book not found for toggling favorite | Book ID: %s | User ID: %s", book_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling favorite status | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise


def toggle_archive(db: Session, book_id: UUID, user_id: UUID) -> BookResponse | None:
    """Toggles the archived status of a book."""
    try:
        book = book_repo.archive(db=db, book_id=book_id, user_id=user_id)
        if book:
            db.commit()
            db.refresh(book)
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            logger.info("Book archived status toggled | Book ID: %s | User ID: %s", book_id, user_id)
            return _book_response(book)
        logger.warning("Book not found for toggling archive | Book ID: %s | User ID: %s", book_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling archive status | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise


def toggle_public(db: Session, book_id: UUID, user_id: UUID) -> BookResponse | None:
    """Toggles the public status of a book."""
    try:
        book = book_repo.public(db=db, book_id=book_id, user_id=user_id)
        if book:
            db.commit()
            db.refresh(book)
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            logger.info("Book public status toggled | Book ID: %s | User ID: %s", book_id, user_id)
            return _book_response(book)
        logger.warning("Book not found for toggling public | Book ID: %s | User ID: %s", book_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling public status | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise


def get_public_books(db: Session, page: int, page_size: int) -> BooksResponse:
    """Retrieves all public books."""
    try:
        books, total_items = book_repo.get_public_books(db=db, page=page, page_size=page_size)
        total_pages = ceil(total_items / page_size) if total_items else 0
        logger.info("Retrieved public books | Page: %d | Page Size: %d | Total Items: %d | Total Pages: %d", page, page_size, total_items, total_pages)
        return BooksResponse(books=[_book_response(book) for book in books], page=page, page_size=page_size, total_items=total_items, total_pages=total_pages)
    except Exception:
        logger.error("Error retrieving public books | Page: %d | Page Size: %d", page, page_size, exc_info=True)
        raise


def _build_books_response(books: list[Book], page: int, page_size: int, total_items: int) -> BooksResponse:
    """Builds a BooksResponse object from the given parameters."""
    try:
        total_pages = ceil(total_items / page_size) if total_items else 0
        logger.info("Built books response | Page: %d | Page Size: %d | Total Items: %d | Total Pages: %d", page, page_size, total_items, total_pages)
        return BooksResponse(books=[_book_response(book) for book in books], page=page, page_size=page_size, total_items=total_items, total_pages=total_pages)
    except Exception:
        logger.error("Error building books response | Page: %d | Page Size: %d", page, page_size, exc_info=True)
        raise


def get_active_books(db: Session, user_id: UUID, page: int, page_size: int) -> BooksResponse:
    """Retrieves all active (non-archived) books for a user."""
    try:
        books, total_items = book_repo.get_active_books(db=db, user_id=user_id, page=page, page_size=page_size)
        logger.info("Retrieved active books for user | User ID: %s | Page: %d | Page Size: %d | Total Items: %d", user_id, page, page_size, total_items)
        return _build_books_response(books=books, page=page, page_size=page_size, total_items=total_items)
    except Exception:
        logger.error("Error retrieving active books for user | User ID: %s | Page: %d | Page Size: %d", user_id, page, page_size, exc_info=True)
        raise


def get_archived_books(db: Session, user_id: UUID, page: int, page_size: int) -> BooksResponse:
    """Retrieves all archived books for a user."""
    try:
        books, total_items = book_repo.get_archived_books(db=db, user_id=user_id, page=page, page_size=page_size)
        logger.info("Retrieved archived books for user | User ID: %s | Page: %d | Page Size: %d | Total Items: %d", user_id, page, page_size, total_items)
        return _build_books_response(books=books, page=page, page_size=page_size, total_items=total_items)
    except Exception:
        logger.error("Error retrieving archived books for user | User ID: %s | Page: %d | Page Size: %d", user_id, page, page_size, exc_info=True)
        raise
