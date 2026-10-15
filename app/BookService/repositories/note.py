from uuid import UUID
from sqlalchemy import select, func
from sqlalchemy.orm import Session
from models.note import Note
from schemas.note import NoteCreate, NoteUpdate

def create(db: Session, note_data: NoteCreate, user_id: UUID) -> Note:
    """ Creates a new note in the database."""
    
    note = Note(user_id=user_id, **note_data.model_dump())

    db.add(note)
    db.flush()

    return note
    
def get_by_id(db: Session, book_id: UUID, note_id: UUID, user_id: UUID) -> Note | None:
    """ Retrieves a note by its ID and user ID."""
    
    statement = select(Note).where(Note.id == note_id, Note.book_id == book_id, Note.user_id == user_id)
    return db.scalar(statement)

def update(db: Session, note: Note, note_data: NoteUpdate) -> Note:
    """ Updates an existing note in the database."""
    
    update_data = note_data.model_dump(exclude_unset=True)
    for key, value in update_data.items():
        setattr(note, key, value)

    db.flush()

    return note

def delete(db: Session, note: Note) -> None:
    """ Deletes a note from the database."""
    
    db.delete(note)
    db.flush()
    
def favorite(db: Session, book_id : UUID, note_id: UUID, user_id: UUID) -> Note:
    """ Marks a note as favorite."""
    
    note = get_by_id(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
    if note:
        note.is_favorite = not note.is_favorite
        db.flush()
    return note

def archive(db: Session, book_id : UUID, note_id: UUID, user_id: UUID) -> Note:
    """ Archives a note."""
    
    note = get_by_id(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
    if note:
        note.is_archived = not note.is_archived
        db.flush()
    return note

def public(db: Session, book_id : UUID, note_id: UUID, user_id: UUID) -> Note:
    """ Makes a note public."""
    
    note = get_by_id(db=db, book_id=book_id, note_id=note_id, user_id=user_id)
    if note:
        note.is_public = not note.is_public
        db.flush()
    return note

def get_public_notes(db: Session, book_id: UUID, page: int = 1, page_size: int = 10) -> tuple[list[Note], int]:
    """Retrieves all public notes for a specific book."""
    
    filters = (Note.book_id == book_id, Note.is_public.is_(True))
    total_items = db.scalar(select(func.count()).select_from(Note).where(*filters)) or 0
    statement = select(Note).where(*filters).offset((page - 1) * page_size).limit(page_size)
    notes = list(db.scalars(statement).all())
    return notes, total_items

def _get_note_by_archived_status(db: Session, user_id: UUID, book_id: UUID, is_archived: bool, page: int, page_size: int) -> tuple[list[Note], int]:
    """ Retrieves notes based on their archived status for a specific book and user."""
    
    filters = (
        Note.user_id == user_id,
        Note.book_id == book_id,
        Note.is_archived.is_(is_archived)
    )
    
    total_items = db.scalar(select(func.count()).select_from(Note).where(*filters)) or 0
    statement = (select(Note).where(*filters).order_by(Note.is_favorite.desc(), Note.updated_at.desc(), Note.id).offset((page - 1) * page_size).limit(page_size))
    
    notes = list(db.scalars(statement).all())
    
    return notes, total_items

def get_active_notes(db: Session, user_id: UUID, book_id: UUID, page: int, page_size: int) -> tuple[list[Note], int]:
    """ Retrieves all active (non-archived) notes for a specific book and user."""
    
    return _get_note_by_archived_status(db=db, user_id=user_id, book_id=book_id, is_archived=False, page=page, page_size=page_size) 

def get_archived_notes(db: Session, user_id: UUID, book_id: UUID, page: int, page_size: int) -> tuple[list[Note], int]:
    """ Retrieves all archived notes for a specific book and user."""
    
    return _get_note_by_archived_status(db=db, user_id=user_id, book_id=book_id, is_archived=True, page=page, page_size=page_size)