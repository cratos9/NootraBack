from uuid import UUID
from sqlalchemy import select
from sqlalchemy.orm import Session
from models.message import Message

def get_messages(db: Session, conversation_id: UUID) -> list[Message]:
    """Retrieves all messages from a conversation."""

    statement = (select(Message).where(Message.conversation_id == conversation_id).order_by(Message.order.asc(), Message.created_at.asc()))
    return list(db.scalars(statement).all())

def get_by_id(db: Session, message_id: UUID, conversation_id: UUID) -> Message | None:
    """Retrieves a message by its ID and conversation."""

    statement = select(Message).where(Message.id == message_id, Message.conversation_id == conversation_id)
    return db.scalar(statement)

def create(db: Session, message_data: dict) -> Message:
    """Creates a new message."""

    message = Message(**message_data)

    db.add(message)
    db.flush()

    return message

def update(db: Session, message: Message, update_data: dict) -> Message:
    """Updates a message with the provided data."""

    for field, value in update_data.items():
        setattr(message, field, value)

    db.flush()

    return message

def delete(db: Session, message: Message) -> None:
    """Deletes a message."""

    db.delete(message)
    db.flush()