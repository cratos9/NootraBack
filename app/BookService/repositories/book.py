from uuid import UUID
from sqlalchemy import select
from sqlalchemy.orm import Session
from models import Book
from schemas.books import BookCreate, BookUpdate

def create(db: Session, book_data: BookCreate, user_id: UUID) -> Book:
    """ Creates a new book in the database."""
    
    book = Book(user_id=user_id, **book_data.model_dump())

    db.add(book)
    db.commit()
    db.refresh(book)

    return book

def get_by_id(db: Session, book_id: UUID, user_id: UUID) -> Book | None:
    """ Retrieves a book by its ID and user ID."""
    
    statement = select(Book).where(Book.id == book_id, Book.user_id == user_id)
    return db.scalar(statement)

def get_all(db: Session, user_id: UUID) -> list[Book]:
    """ Retrieves all books for a specific user."""
    
    statement = select(Book).where(Book.user_id == user_id)
    return list(db.scalars(statement).all())

def update(db: Session, book: Book, book_data: BookUpdate) -> Book:
    """ Updates an existing book in the database."""
    
    update_data = book_data.model_dump(exclude_unset=True)
    for key, value in update_data.items():
        setattr(book, key, value)

    db.commit()
    db.refresh(book)

    return book

def delete(db: Session, book: Book) -> None:
    """ Deletes a book from the database."""
    
    db.delete(book)
    db.commit()