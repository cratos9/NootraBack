from uuid import UUID
from fastapi import APIRouter, Depends, HTTPException, Request, status
from sqlalchemy.orm import Session
from database.session import get_db
from api.dependencies.auth import get_current_user
from core.rate_limit import limiter
from schemas.message import MessageCreate, MessageUpdate, MessageResponse, MessageListResponse
from services import messages as message_service
import logging

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/conversations/{conversation_id}/messages", tags=["Messages"],)

@router.get("", response_model=MessageListResponse, summary="Get all messages in a conversation",)
@limiter.limit("50/minute")
def get_messages(request: Request, conversation_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user),):
    """Retrieves every message in a conversation."""

    messages = message_service.get_messages(db=db, conversation_id=conversation_id, user_id=current_user,)
    if messages is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversation not found",)
    logger.info("Messages retrieved | Conversation ID: %s | User ID: %s | Count: %d", conversation_id, current_user, len(messages.messages),)

    return messages

@router.get("/{message_id}", response_model=MessageResponse, summary="Get a message",)
@limiter.limit("50/minute")
def get_message(request: Request, conversation_id: UUID, message_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user),):
    """Retrieves a specific message from a conversation."""

    message = message_service.get_message(db=db, message_id=message_id, conversation_id=conversation_id, user_id=current_user,)
    if not message:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Message not found",)
    logger.info("Message retrieved | Message ID: %s | User ID: %s", message_id, current_user,)

    return message

@router.post("", response_model=MessageResponse, status_code=status.HTTP_201_CREATED, summary="Ask the AI",)
@limiter.limit("10/minute")
async def ask_ai(request: Request, conversation_id: UUID, message_data: MessageCreate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user),):
    """Sends a question to the AI and stores its response."""

    message = await message_service.ask_ai(db=db, conversation_id=conversation_id, user_id=current_user, query_text=message_data.query_text,)
    if not message:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversation not found",)
    logger.info("AI message created | Message ID: %s | Conversation ID: %s | User ID: %s", message.id, conversation_id, current_user,)

    return message

@router.put("/{message_id}", response_model=MessageResponse, summary="Update a message",)
@limiter.limit("20/minute")
def update_message(request: Request, conversation_id: UUID, message_id: UUID, message_data: MessageUpdate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user),):
    """Updates editable fields of a message."""

    message = message_service.update_message(db=db, message_id=message_id, conversation_id=conversation_id, user_id=current_user, data=message_data,)
    if not message:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Message not found",)
    logger.info("Message updated | Message ID: %s | Conversation ID: %s | User ID: %s", message_id, conversation_id, current_user,)

    return message

@router.delete("/{message_id}", response_model=bool, summary="Delete a message",)
@limiter.limit("20/minute")
def delete_message(request: Request, conversation_id: UUID, message_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user),):
    """Deletes a message from a conversation."""

    deleted = message_service.delete_message(db=db, message_id=message_id, conversation_id=conversation_id, user_id=current_user,)
    if not deleted:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Message not found",)
    logger.info("Message deleted | Message ID: %s | Conversation ID: %s | User ID: %s", message_id, conversation_id, current_user,)

    return deleted