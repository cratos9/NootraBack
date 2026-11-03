from uuid import UUID
from fastapi import APIRouter, Depends, HTTPException, Request, Query, status
from sqlalchemy.orm import Session
from database.session import get_db
from api.dependencies.auth import get_current_user
from core.rate_limit import limiter
from schemas.conversation import ConversationCreate, ConversationUpdate, ConversationResponse, ConversationListResponse
from services import conversation as conversation_service
import logging

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/conversations", tags=["Conversations"])

@router.post("", response_model=ConversationResponse, status_code=status.HTTP_201_CREATED, summary="Create a conversation")
@limiter.limit("20/minute")
def create_conversation(request: Request, conversation_data: ConversationCreate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Creates a new conversation for the current user."""

    conversation = conversation_service.create_conversation(db=db, user_id=current_user, data=conversation_data)
    logger.info("Conversation created | Conversation ID: %s | User ID: %s", conversation.id, current_user)

    return conversation

@router.get("", response_model=ConversationListResponse, summary="Get active conversations")
@limiter.limit("50/minute")
def get_conversations(request: Request, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves active conversations with pagination."""

    conversations = conversation_service.get_conversations(db=db, user_id=current_user, page=page, page_size=page_size)
    logger.info("Conversations retrieved | User ID: %s | Page: %d | Count: %d", current_user, page, len(conversations.conversations))

    return conversations

@router.get("/archived", response_model=ConversationListResponse, summary="Get archived conversations")
@limiter.limit("20/minute")
def get_archived_conversations(request: Request, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves archived conversations with pagination."""

    conversations = conversation_service.get_archived_conversations(db=db, user_id=current_user, page=page, page_size=page_size)
    logger.info("Archived conversations retrieved | User ID: %s | Page: %d | Count: %d", current_user, page, len(conversations.conversations))

    return conversations


@router.get("/favorites", response_model=ConversationListResponse, summary="Get favorite conversations")
@limiter.limit("20/minute")
def get_favorite_conversations(request: Request, page: int = Query(1, ge=1), page_size: int = Query(25, ge=1, le=100), db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves favorite conversations, including archived ones."""

    conversations = conversation_service.get_favorite_conversations(db=db, user_id=current_user, page=page, page_size=page_size)
    logger.info("Favorite conversations retrieved | User ID: %s | Page: %d | Count: %d", current_user, page, len(conversations.conversations))

    return conversations

@router.get("/{conversation_id}", response_model=ConversationResponse, summary="Get a conversation")
@limiter.limit("50/minute")
def get_conversation(request: Request, conversation_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Retrieves a specific conversation."""

    conversation = conversation_service.get_conversation(db=db, conversation_id=conversation_id, user_id=current_user)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversation not found")
    logger.info("Conversation retrieved | Conversation ID: %s | User ID: %s",conversation_id, current_user)

    return conversation

@router.put("/{conversation_id}", response_model=ConversationResponse, summary="Update a conversation")
@limiter.limit("20/minute")
def update_conversation(request: Request, conversation_id: UUID, conversation_data: ConversationUpdate, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Updates a conversation's title or status fields."""

    conversation = conversation_service.update_conversation(db=db, conversation_id=conversation_id, user_id=current_user, data=conversation_data)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversation not found")
    logger.info(
        "Conversation updated | Conversation ID: %s | User ID: %s",conversation_id, current_user)

    return conversation

@router.delete("/{conversation_id}", response_model=bool, summary="Delete a conversation")
@limiter.limit("20/minute")
def delete_conversation(request: Request, conversation_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Deletes a conversation and its messages."""

    deleted = conversation_service.delete_conversation(db=db, conversation_id=conversation_id, user_id=current_user)
    if not deleted:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversation not found")
    logger.info("Conversation deleted | Conversation ID: %s | User ID: %s", conversation_id, current_user)

    return deleted

@router.post("/{conversation_id}/favorite", response_model=ConversationResponse, summary="Toggle conversation favorite")
@limiter.limit("20/minute")
def toggle_favorite(request: Request, conversation_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Toggles a conversation's favorite status."""

    conversation = conversation_service.toggle_favorite(db=db, conversation_id=conversation_id, user_id=current_user)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversation not found")
    logger.info("Conversation favorite toggled | Conversation ID: %s | User ID: %s", conversation_id, current_user)

    return conversation

@router.post("/{conversation_id}/archive", response_model=ConversationResponse, summary="Toggle conversation archive")
@limiter.limit("20/minute")
def toggle_archive(request: Request, conversation_id: UUID, db: Session = Depends(get_db), current_user: UUID = Depends(get_current_user)):
    """Toggles a conversation's archived status."""

    conversation = conversation_service.toggle_archive(db=db, conversation_id=conversation_id, user_id=current_user)
    if not conversation:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Conversation not found")
    logger.info("Conversation archive toggled | Conversation ID: %s | User ID: %s", conversation_id, current_user)

    return conversation