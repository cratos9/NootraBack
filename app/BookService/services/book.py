from uuid import UUID
from sqlalchemy.orm import Session
from models.book import Book
from repositories import book as book_repo
from schemas.book import BookCreate, BookUpdate

def create_book(db:Session, book_data: BookCreate, user_id: UUID) -> Book:
    """Creates a new book in the database."""
    return book_repo.create(db=db, book_data=book_data, user_id=user_id)

def get_book_by_id(db:Session, book_id: UUID, user_id: UUID) -> Book | None:
    """Retrieves a book by its ID and user ID."""
    return book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)

def get_all_books(db:Session, user_id: UUID) -> list[Book]:
    """Retrieves all books for a specific user."""
    return book_repo.get_all(db=db, user_id=user_id)

def update_book(db: Session, book_id: UUID, book_data: BookUpdate, user_id: UUID) -> Book | None:
    """Updates an existing book in the database."""
    book = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)
    if book:
        return book_repo.update(db=db, book=book, book_data=book_data)
    return None

def delete_book(db: Session, book_id: UUID, user_id: UUID) -> bool:
    """Deletes a book from the database."""
    book = book_repo.get_by_id(db=db, book_id=book_id, user_id=user_id)
    if book:
        book_repo.delete(db=db, book=book)
        return True
    return False