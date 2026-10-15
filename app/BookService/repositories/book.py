from uuid import UUID
from sqlalchemy import select, func
from sqlalchemy.orm import Session
from models.book import Book
from schemas.book import BookCreate, BookUpdate
from math import ceil

def create(db: Session, book_data: BookCreate, user_id: UUID) -> Book:
    """ Creates a new book in the database."""
    
    book = Book(user_id=user_id, **book_data.model_dump())

    db.add(book)
    db.flush()

    return book

def get_by_id(db: Session, book_id: UUID, user_id: UUID) -> Book | None:
    """ Retrieves a book by its ID and user ID."""
    
    statement = select(Book).where(Book.id == book_id, Book.user_id == user_id)
    return db.scalar(statement)

def update(db: Session, book: Book, book_data: BookUpdate) -> Book:
    """ Updates an existing book in the database."""
    
    update_data = book_data.model_dump(exclude_unset=True)
    for key, value in update_data.items():
        setattr(book, key, value)

    db.flush()

    return book

def delete(db: Session, book: Book) -> None:
    """ Deletes a book from the database."""
    
    db.delete(book)
    db.flush()
    
def favorite(db: Session, book_id: UUID, user_id: UUID) -> Book:
    """ Marks a book as favorite."""
    
    book = get_by_id(db=db, book_id=book_id, user_id=user_id)
    if book:
        book.is_favorite = not book.is_favorite
        db.flush()
    return book

def archive(db: Session, book_id: UUID, user_id: UUID) -> Book:
    """ Archives a book."""
    
    book = get_by_id(db=db, book_id=book_id, user_id=user_id)
    if book:
        book.is_archived = not book.is_archived
        db.flush()
    return book

def public(db: Session, book_id: UUID, user_id: UUID) -> Book:
    """ Makes a book public."""
    
    book = get_by_id(db=db, book_id=book_id, user_id=user_id)
    if book:
        book.is_public = not book.is_public
        db.flush()
    return book

def get_public_books(db: Session, page: int, page_size: int) -> tuple[list[Book], int]:
    """ Retrieves all public books."""
    total_items = db.scalar(select(func.count()).select_from(Book).where(Book.is_public.is_(True))) or 0
    statement = (select(Book).where(Book.is_public.is_(True)).offset((page - 1) * page_size).limit(page_size))
    books = list(db.scalars(statement).all())
    return books, total_items

def _get_book_by_archived_status(db: Session, user_id: UUID, page:int, page_size:int, is_archived: bool) -> tuple[list[Book], int]:
    """ Retrieves books based on their archived status."""
    filters = (
        Book.user_id == user_id,
        Book.is_archived.is_(is_archived)
    )
    
    total_items = db.scalar(select(func.count()).select_from(Book).where(*filters)) or 0
    statement = (select(Book).where(*filters).order_by(Book.is_favorite.desc(), Book.updated_at.desc(), Book.id).offset((page - 1) * page_size).limit(page_size))
    
    books = list(db.scalars(statement).all())
    
    return books, total_items

def get_active_books(db: Session, user_id: UUID, page: int, page_size: int) -> tuple[list[Book], int]:
    """ Retrieves all active (non-archived) books for a user."""
    return _get_book_by_archived_status(db=db, user_id=user_id, page=page, page_size=page_size, is_archived=False)

def get_archived_books(db: Session, user_id: UUID, page: int, page_size: int) -> tuple[list[Book], int]:
    """ Retrieves all archived books for a user."""
    return _get_book_by_archived_status(db=db, user_id=user_id, page=page, page_size=page_size, is_archived=True)