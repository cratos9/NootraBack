from uuid import UUID
from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session
from database.session import get_db
from schemas.book import BookCreate, BookUpdate, BookResponse
from services import book as book_service
from api.dependencies.auth import get_current_user

router = APIRouter(prefix="/books", tags=["Books"])

@router.get("/prueba")
def prueba(current_user: UUID = Depends(get_current_user)):
    """Endpoint to test authentication and retrieve the current user's UUID."""
    return {"message": "Authenticated successfully", "user_id": str(current_user)}

@router.post("", response_model=BookResponse, status_code=status.HTTP_201_CREATED)
def create_book(book_data: BookCreate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Creates a new book in the database."""
    user_id = current_user
    return book_service.create_book(db=db, book_data=book_data, user_id=user_id)

@router.get("", response_model=list[BookResponse])
def get_books(db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves all books from the database."""
    user_id = current_user
    return book_service.get_all_books(db=db, user_id=user_id)

@router.get("/{book_id}", response_model=BookResponse)
def get_book(book_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves a book by its ID."""
    user_id = current_user
    book = book_service.get_book_by_id(db=db, book_id=book_id, user_id=user_id)
    if not book:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    return book

@router.put("/{book_id}", response_model=BookResponse)
def update_book(book_id: UUID, book_data: BookUpdate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Updates an existing book in the database."""
    user_id = current_user
    book = book_service.update_book(db=db, book_id=book_id, book_data=book_data, user_id=user_id)
    if not book:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")
    return book

@router.delete("/{book_id}", response_model=bool)
def delete_book(book_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Deletes a book from the database."""
    user_id = current_user
    deleted = book_service.delete_book(db=db, book_id=book_id, user_id=user_id)
    if not deleted:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Book not found")