from uuid import UUID
from sqlalchemy import func, select
from sqlalchemy.orm import Session
from models.conversation import Conversation

def get_by_id(db: Session, conversation_id: UUID, user_id: UUID) -> Conversation | None:
    """Retrieves a conversation by its ID and owner."""

    statement = select(Conversation).where(Conversation.id == conversation_id, Conversation.user_id == user_id)

    return db.scalar(statement)

def create(db: Session, user_id: UUID, title: str) -> Conversation:
    """Creates a new conversation."""

    conversation = Conversation(user_id=user_id, title=title)

    db.add(conversation)
    db.flush()

    return conversation

def update(db: Session, conversation: Conversation, update_data: dict) -> Conversation:
    """Updates a conversation with the provided data."""

    for field, value in update_data.items():
        setattr(conversation, field, value)

    db.flush()

    return conversation

def delete(db: Session, conversation: Conversation) -> None:
    """Deletes a conversation."""

    db.delete(conversation)
    db.flush()

def toggle_favorite(db: Session, conversation_id: UUID, user_id: UUID) -> Conversation | None:
    """Toggles the favorite status of a conversation."""

    conversation = get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)

    if conversation:
        conversation.is_favorite = not conversation.is_favorite
        db.flush()

    return conversation

def toggle_archive(db: Session, conversation_id: UUID, user_id: UUID) -> Conversation | None:
    """Toggles the archived status of a conversation."""

    conversation = get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)

    if conversation:
        conversation.is_archived = not conversation.is_archived
        db.flush()

    return conversation

def _get_conversations_by_status(db: Session, user_id: UUID, page: int, page_size: int, is_archived: bool | None = None, favorites_only: bool = False) -> tuple[list[Conversation], int]:
    """Retrieves conversations based on their status."""

    filters = [Conversation.user_id == user_id,]

    if is_archived is not None:
        filters.append(Conversation.is_archived.is_(is_archived))

    if favorites_only:
        filters.append(Conversation.is_favorite.is_(True))

    total_items = db.scalar(select(func.count()).select_from(Conversation).where(*filters)) or 0
    statement = (select(Conversation).where(*filters).order_by(Conversation.is_favorite.desc(), Conversation.updated_at.desc(), Conversation.id).offset((page - 1) * page_size).limit(page_size))
    conversations = list(db.scalars(statement).all())

    return conversations, total_items

def get_active_conversations(db: Session, user_id: UUID, page: int, page_size: int) -> tuple[list[Conversation], int]:
    """Retrieves all active conversations for a user."""
    
    return _get_conversations_by_status(db=db, user_id=user_id, page=page, page_size=page_size, is_archived=False)

def get_archived_conversations(db: Session, user_id: UUID, page: int, page_size: int) -> tuple[list[Conversation], int]:
    """Retrieves all archived conversations for a user."""

    return _get_conversations_by_status(db=db, user_id=user_id, page=page, page_size=page_size, is_archived=True)

def get_favorite_conversations(db: Session, user_id: UUID, page: int, page_size: int) -> tuple[list[Conversation], int]:
    """Retrieves all favorite conversations for a user."""

    return _get_conversations_by_status(db=db, user_id=user_id, page=page, page_size=page_size, favorites_only=True)