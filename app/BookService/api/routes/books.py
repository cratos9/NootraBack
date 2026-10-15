from uuid import UUID
from fastapi import APIRouter, Depends, HTTPException, status, Request, Query
from sqlalchemy.orm import Session
from database.session import get_db
from schemas.book import BookCreate, BookUpdate, BookResponse, BookPublish, BookArchive, BookFavorite, BooksResponse
from services import book as book_service
from api.dependencies.auth import get_current_user
from core.rate_limit import limiter
import logging

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/books", tags=["Books"])

@router.get("/prueba", summary="Test endpoint for authentication", description="This endpoint is used to test authentication and retrieve the current user's UUID.")
@limiter.limit("5/minute")
def prueba(request: Request, current_user: UUID = Depends(get_current_user)):
    """Endpoint to test authentication and retrieve the current user's UUID."""
    logger.info("Authenticated request | User ID: %s", current_user)
    return {"message": "Authenticated successfully", "user_id": str(current_user)}

@router.post("", response_model=BookResponse, status_code=status.HTTP_201_CREATED, summary="Create a new book", description="Creates a new book in the database and returns the created book's details.")
@limiter.limit("20/minute")
def create_book(request: Request, book_data: BookCreate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Creates a new book in the database."""
    created_book = book_service.create_book(db=db, book_data=book_data, user_id=current_user)
    logger.info("Book created | Book ID: %s | User ID: %s", created_book.id, current_user)
    return created_book

@router.get("", response_model=BooksResponse, summary="Get all books", description="Retrieves all active books from the database with pagination.")
@limiter.limit("50/minute")
def get_books(request: Request,page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves all books from the database."""
    books = book_service.get_active_books(db=db, user_id=current_user, page=page, page_size=page_size)
    logger.info("Books retrieved | User ID: %s | Count: %d", current_user, len(books))
    return books

@router.get("/archived", response_model=BooksResponse, summary="Get archived books", description="Retrieves archived books from the database with pagination.")
@limiter.limit("20/minute")
def get_archived_books(request: Request, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves archived books with pagination."""
    books = book_service.get_archived_books(db=db, user_id=current_user, page=page, page_size=page_size)
    logger.info("Archived books retrieved | User ID: %s | Page: %d | Count: %d", current_user, page, len(books))
    return books

@router.get("/{book_id}", response_model=BookResponse, summary="Get a book by ID", description="Retrieves a specific book by its ID.")
@limiter.limit("50/minute")
def get_book(request: Request, book_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves a book by its ID."""
    book = book_service.get_book_by_id(db=db, book_id=book_id, user_id=current_user)
    if not book:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    logger.info("Book retrieved | Book ID: %s | User ID: %s", book_id, current_user)
    return book

@router.put("/{book_id}", response_model=BookResponse, summary="Update a book", description="Updates an existing book in the database.")
@limiter.limit("20/minute")
def update_book(request: Request, book_id: UUID, book_data: BookUpdate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Updates an existing book in the database."""
    book = book_service.update_book(db=db, book_id=book_id, book_data=book_data, user_id=current_user)
    if not book:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    logger.info("Book updated | Book ID: %s | User ID: %s", book_id, current_user)
    return book

@router.delete("/{book_id}", response_model=bool, summary="Delete a book", description="Deletes a book from the database.")
@limiter.limit("50/minute")
def delete_book(request: Request, book_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Deletes a book from the database."""
    deleted = book_service.delete_book(db=db, book_id=book_id, user_id=current_user)
    if not deleted:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    logger.info("Book deleted | Book ID: %s | User ID: %s", book_id, current_user)
    return deleted

@router.post("/favorite", response_model=BookResponse, summary="Mark a book as favorite", description="Marks a specific book as favorite for the current user.")
@limiter.limit("20/minute")
def favorite_book(request: Request, book_id: BookFavorite, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Marks a book as favorite."""
    book = book_service.favorite_book(db=db, book_id=book_id, user_id=current_user)
    if not book:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    logger.info("Book favorited | Book ID: %s | User ID: %s", book_id, current_user)
    return book

@router.post("/archive", response_model=BookResponse, summary="Archive a book", description="Archives a specific book for the current user.")
@limiter.limit("20/minute")
def archive_book(request: Request, book_id: BookArchive, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Archives a book."""
    book = book_service.archive_book(db=db, book_id=book_id, user_id=current_user)
    if not book:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    logger.info("Book archived | Book ID: %s | User ID: %s", book_id, current_user)
    return book

@router.post("/publish", response_model=BookResponse, summary="Make a book public", description="Makes a specific book public for all users.")
@limiter.limit("20/minute")
def public_book(request: Request, book_id: BookPublish, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Makes a book public."""
    book = book_service.public_book(db=db, book_id=book_id, user_id=current_user)
    if not book:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    logger.info("Book made public | Book ID: %s | User ID: %s", book_id, current_user)
    return book

@router.get("/public_books", response_model=BooksResponse, summary="Get public books", description="Retrieves public books from the database with pagination.")
@limiter.limit("20/minute")
def get_public_books(request: Request, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves public books with pagination."""
    books = book_service.get_public_books(db=db, page=page, page_size=page_size)
    logger.info("Public books retrieved | Page: %d | Count: %d", page, len(books))
    return books