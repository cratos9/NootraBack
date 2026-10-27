from uuid import UUID
from sqlalchemy import select, func
from sqlalchemy.orm import Session
from models.attachment import Attachment
from schemas.attachment import AttachmentUpdate
from math import ceil


def create(db: Session, attachment: Attachment) -> Attachment:
    """Creates a new attachment in the database."""

    db.add(attachment)
    db.flush()

    return attachment


def get_by_id(db: Session, attachment_id: UUID, user_id: UUID) -> Attachment | None:
    """Retrieves an attachment by its ID and user ID."""

    statement = select(Attachment).where(Attachment.id == attachment_id, Attachment.user_id == user_id)

    return db.scalar(statement)


def update(db: Session, attachment: Attachment, attachment_data: AttachmentUpdate) -> Attachment:
    """Updates an existing attachment in the database."""

    update_data = attachment_data.model_dump(exclude_unset=True)
    for key, value in update_data.items():
        setattr(attachment, key, value)

    db.flush()

    return attachment


def delete(db: Session, attachment: Attachment) -> None:
    """Deletes an attachment from the database."""

    db.delete(attachment)
    db.flush()


def favorite(db: Session, attachment_id: UUID, user_id: UUID) -> Attachment | None:
    """Toggles an attachment's favorite status."""

    attachment = get_by_id(db=db, attachment_id=attachment_id, user_id=user_id)
    if attachment:
        attachment.is_favorite = not attachment.is_favorite
        db.flush()

    return attachment


def public(db: Session, attachment_id: UUID, user_id: UUID) -> Attachment | None:
    """Toggles an attachment's public status."""

    attachment = get_by_id(db=db, attachment_id=attachment_id, user_id=user_id)

    if attachment:
        attachment.is_public = not attachment.is_public
        db.flush()

    return attachment


def get_all(db: Session, user_id: UUID, page: int, page_size: int, book_id: UUID | None = None, file_type: str | None = None, mime_type: str | None = None, is_public: bool | None = None, is_favorite: bool | None = None, search: str | None = None) -> tuple[list[Attachment], int]:
    """Retrieves a user's attachments with optional filters."""

    filters = [Attachment.user_id == user_id]

    if book_id is not None:
        filters.append(Attachment.book_id == book_id)

    if file_type is not None:
        filters.append(Attachment.file_type == file_type)

    if mime_type is not None:
        filters.append(Attachment.mime_type == mime_type)

    if is_public is not None:
        filters.append(Attachment.is_public.is_(is_public))

    if is_favorite is not None:
        filters.append(Attachment.is_favorite.is_(is_favorite))

    if search is not None:
        filters.append(Attachment.original_filename.ilike(f"%{search}%"))

    total_items = db.scalar(select(func.count()).select_from(Attachment).where(*filters)) or 0
    statement = (select(Attachment).where(*filters).order_by(Attachment.is_favorite.desc(), Attachment.created_at.desc(), Attachment.id).offset((page - 1) * page_size).limit(page_size))
    attachments = list(db.scalars(statement).all())

    return attachments, total_items


def get_public_attachments(db: Session, page: int, page_size: int, file_type: str | None = None, mime_type: str | None = None, search: str | None = None) -> tuple[list[Attachment], int]:
    """Retrieves public attachments with optional filters."""

    filters = [Attachment.is_public.is_(True)]

    if file_type is not None:
        filters.append(Attachment.file_type == file_type)

    if mime_type is not None:
        filters.append(Attachment.mime_type == mime_type)

    if search is not None:
        filters.append(Attachment.original_filename.ilike(f"%{search}%"))

    total_items = db.scalar(select(func.count()).select_from(Attachment).where(*filters)) or 0
    statement = (select(Attachment).where(*filters).order_by(Attachment.created_at.desc(), Attachment.id).offset((page - 1) * page_size).limit(page_size))
    attachments = list(db.scalars(statement).all())

    return attachments, total_items