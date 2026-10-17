from uuid import UUID
from sqlalchemy.orm import Session
from models.book import Book
from repositories import book as book_repo
from math import ceil
from redis.exceptions import RedisError
import json
from database.redis import redis_client
from core.config import settings
from schemas.book import BookCreate, BookUpdate, BookResponse, BooksResponse
import logging

logger = logging.getLogger(__name__)

def invalidate_book_cache(book_id: UUID, user_id: UUID):
    """Invalidates the cache for a specific book."""
    cache_key = f"book:{book_id}:{user_id}"
    try:
        redis_client.delete(cache_key)
        logger.info("Cache invalidated for book ID: %s | User ID: %s", book_id, user_id)
    except RedisError as e:
        logger.error("Redis error while invalidating cache for book ID: %s | User ID: %s | Error: %s", book_id, user_id, e)

def create_book(db:Session, book_data: BookCreate, user_id: UUID) -> Book:
    """Creates a new book in the database."""
    try:
        book = book_repo.create(db=db, book_data=book_data, user_id=user_id)
        db.commit()
        db.refresh(book)
        logger.info("Book created successfully | Book ID: %s | User ID: %s", book.id, user_id)
        return book
    except Exception:
        db.rollback()
        logger.error("Error creating book | User ID: %s", user_id, exc_info=True)
        raise

def get_book_by_id(db:Session, book_id: UUID, user_id: UUID) -> Book | None:
    """Retrieves a book by its ID and user ID."""
    cache_key = f"book:{book_id}:{user_id}"
    try:
        cached_book = redis_client.get(cache_key)
        if cached_book:
            logger.info("Cache hit for book ID: %s | User ID: %s", book_id, user_id)
            return BookResponse.model_validate_json(cached_book)
    except RedisError as e:
        logger.error("Redis error while fetching book ID: %s | User ID: %s | Error: %s", book_id, user_id, e)
    response = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)
    if response:
        try:
            redis_client.setex(cache_key, settings.redis_cache_expire, BookResponse.model_validate(response).model_dump_json())
        except RedisError as e:
            logger.error("Redis error while caching book ID: %s | User ID: %s | Error: %s", book_id, user_id, e)
    return response

def update_book(db: Session, book_id: UUID, book_data: BookUpdate, user_id: UUID) -> Book | None:
    """Updates an existing book in the database."""
    try:
        book = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)
        if book:
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            updated_book = book_repo.update(db=db, book=book, book_data=book_data)
            db.commit()
            db.refresh(updated_book)
            logger.info("Book updated successfully | Book ID: %s | User ID: %s", book_id, user_id)
            return updated_book
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
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            book_repo.delete(db=db, book=book)
            db.commit()
            logger.info("Book deleted successfully | Book ID: %s | User ID: %s", book_id, user_id)
            return True
        logger.warning("Book not found for deletion | Book ID: %s | User ID: %s", book_id, user_id)
        return False
    except Exception:
        db.rollback()
        logger.error("Error deleting book | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise
    
def toggle_favorite(db: Session, book_id: UUID, user_id: UUID) -> Book | None:
    """Toggles the favorite status of a book."""
    try:
        book = book_repo.favorite(db=db, book_id=book_id, user_id=user_id)
        if book:
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            db.commit()
            db.refresh(book)
            logger.info("Book favorite status toggled | Book ID: %s | User ID: %s", book_id, user_id)
            return book
        logger.warning("Book not found for toggling favorite | Book ID: %s | User ID: %s", book_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling favorite status | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise
    
def toggle_archive(db: Session, book_id: UUID, user_id: UUID) -> Book | None:
    """Toggles the archived status of a book."""
    try:
        book = book_repo.archive(db=db, book_id=book_id, user_id=user_id)
        if book:
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            db.commit()
            db.refresh(book)
            logger.info("Book archived status toggled | Book ID: %s | User ID: %s", book_id, user_id)
            return book
        logger.warning("Book not found for toggling archive | Book ID: %s | User ID: %s", book_id, user_id)
        return None
    except Exception:
        db.rollback()
        logger.error("Error toggling archive status | Book ID: %s | User ID: %s", book_id, user_id, exc_info=True)
        raise
    
def toggle_public(db: Session, book_id: UUID, user_id: UUID) -> Book | None:
    """Toggles the public status of a book."""
    try:
        book = book_repo.public(db=db, book_id=book_id, user_id=user_id)
        if book:
            invalidate_book_cache(book_id=book_id, user_id=user_id)
            db.commit()
            db.refresh(book)
            logger.info("Book public status toggled | Book ID: %s | User ID: %s", book_id, user_id)
            return book
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
        return BooksResponse( books=books, page=page, page_size=page_size, total_items=total_items, total_pages=total_pages)
    except Exception:
        logger.error("Error retrieving public books | Page: %d | Page Size: %d", page, page_size, exc_info=True)
        raise

def _build_books_response(books: list[Book], page: int, page_size: int, total_items: int) -> BooksResponse:
    """Builds a BooksResponse object from the given parameters."""
    try:
        total_pages = ceil(total_items / page_size) if total_items else 0
        logger.info("Built books response | Page: %d | Page Size: %d | Total Items: %d | Total Pages: %d", page, page_size, total_items, total_pages)
        return BooksResponse(books=books, page=page, page_size=page_size, total_items=total_items, total_pages=total_pages)
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