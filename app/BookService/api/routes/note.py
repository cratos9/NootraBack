from uuid import UUID
from fastapi import APIRouter, Depends, HTTPException, status, Request, Query
from sqlalchemy.orm import Session
from database.session import get_db
from schemas.note import NoteCreate, NoteUpdate, NoteResponse, NotesResponse, NoteArchive, NoteFavorite, NotePublish
from services import note as note_service
from api.dependencies.auth import get_current_user
from core.rate_limit import limiter
import logging

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/notes", tags=["Notes"])

@router.post("", response_model=NoteResponse, status_code=status.HTTP_201_CREATED, summary="Create a new note", description="Creates a new note in the database and returns the created note's details.")
@limiter.limit("20/minute")
def create_note(request: Request, note_data: NoteCreate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Creates a new note in the database."""
    created_note = note_service.create_note(db=db, note_data=note_data, user_id=current_user)
    logger.info("Note created | Note ID: %s | User ID: %s", created_note.id, current_user)
    return created_note

@router.get("/public_notes", response_model=NotesResponse, summary="Get public notes", description="Retrieves public notes from the database with pagination.")
@limiter.limit("20/minute")
def get_public_notes(request: Request, book_id: UUID, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves public notes from the database."""
    notes = note_service.get_public_notes(db=db, book_id=book_id, page=page, page_size=page_size)
    logger.info("Public notes retrieved | Book ID: %s | Page: %d | Count: %d", book_id, page, len(notes.notes))
    return notes

@router.get("/{book_id}", response_model=NotesResponse, summary="Get all notes", description="Retrieves all active notes from the database with pagination.")
@limiter.limit("50/minute")
def get_notes(request: Request, book_id: UUID, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves all notes from the database."""
    notes = note_service.get_active_notes(db=db, book_id=book_id, user_id=current_user, page=page, page_size=page_size)
    logger.info("Notes retrieved | Book ID: %s | User ID: %s | Count: %d", book_id, current_user, len(notes.notes))
    return notes

@router.get("/{book_id}/archived", response_model=NotesResponse, summary="Get archived notes", description="Retrieves archived notes from the database with pagination.")
@limiter.limit("20/minute")
def get_archived_notes(request: Request, book_id: UUID, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves archived notes with pagination."""
    notes = note_service.get_archived_notes(db=db, book_id=book_id, user_id=current_user, page=page, page_size=page_size)
    logger.info("Archived notes retrieved | Book ID: %s | User ID: %s | Page: %d | Count: %d", book_id, current_user, page, len(notes.notes))
    return notes

@router.get("/{book_id}/{note_id}", response_model=NoteResponse, summary="Get a note by ID", description="Retrieves a specific note by its ID.")
@limiter.limit("50/minute")
def get_note(request: Request, book_id: UUID, note_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves a note by its ID."""
    note = note_service.get_note_by_id(db=db, book_id=book_id, note_id=note_id, user_id=current_user)
    if not note:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Note not found")
    logger.info("Note retrieved | Note ID: %s | Book ID: %s | User ID: %s", note_id, book_id, current_user)
    return note

@router.put("/{book_id}/{note_id}", response_model=NoteResponse, summary="Update a note", description="Updates an existing note in the database.")
@limiter.limit("20/minute")
def update_note(request: Request, book_id: UUID, note_id: UUID, note_data: NoteUpdate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Updates an existing note in the database."""
    note = note_service.update_note(db=db, book_id=book_id, note_id=note_id, note_data=note_data, user_id=current_user)
    if not note:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Note not found")
    logger.info("Note updated | Note ID: %s | Book ID: %s | User ID: %s", note_id, book_id, current_user)
    return note

@router.delete("/{book_id}/{note_id}", response_model=bool, summary="Delete a note", description="Deletes a note from the database.")
@limiter.limit("50/minute")
def delete_note(request: Request, book_id: UUID, note_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Deletes a note from the database."""
    success = note_service.delete_note(db=db, book_id=book_id, note_id=note_id, user_id=current_user)
    if not success:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Note not found")
    logger.info("Note deleted | Note ID: %s | Book ID: %s | User ID: %s", note_id, book_id, current_user)
    return success

@router.post("/favorite", response_model=NoteResponse, summary="Mark a note as favorite", description="Marks a note as favorite in the database.")
@limiter.limit("20/minute")
def favorite_note(request: Request, IDs: NoteFavorite, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Marks a note as favorite in the database."""
    note = note_service.toggle_favorite(db=db, book_id=IDs.book_id, note_id=IDs.id, user_id=current_user)
    if not note:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Note not found")
    logger.info("Note marked as favorite | Note ID: %s | Book ID: %s | User ID: %s", IDs.id, IDs.book_id, current_user)
    return note

@router.post("/archive", response_model=NoteResponse, summary="Archive a note", description="Archives a note in the database.")
@limiter.limit("20/minute")
def archive_note(request: Request, IDs: NoteArchive, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Archives a note in the database."""
    note = note_service.toggle_archive(db=db, book_id=IDs.book_id, note_id=IDs.id, user_id=current_user)
    if not note:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Note not found")
    logger.info("Note archived | Note ID: %s | Book ID: %s | User ID: %s", IDs.id, IDs.book_id, current_user)
    return note

@router.post("/publish", response_model=NoteResponse, summary="Publish a note", description="Publishes a note in the database.")
@limiter.limit("20/minute")
def publish_note(request: Request, IDs: NotePublish, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Publishes a note in the database."""
    note = note_service.toggle_public(db=db, book_id=IDs.book_id, note_id=IDs.id, user_id=current_user)
    if not note:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Note not found")
    logger.info("Note published | Note ID: %s | Book ID: %s | User ID: %s", IDs.id, IDs.book_id, current_user)
    return note
