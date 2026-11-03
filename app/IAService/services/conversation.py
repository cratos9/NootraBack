from math import ceil
from uuid import UUID
from sqlalchemy.orm import Session
from models.conversation import Conversation
from repositories import conversation as conversation_repo
from schemas.conversation import ConversationCreate, ConversationUpdate, ConversationResponse, ConversationListResponse
import logging

logger = logging.getLogger(__name__)

def _conversation_response(conversation: Conversation) -> ConversationResponse:
    """Builds a ConversationResponse object."""

    try:
        return ConversationResponse.model_validate(conversation)
    except Exception:
        logger.error("Error building conversation response | " "Conversation ID: %s", conversation.id, exc_info=True)
        raise

def _build_conversations_response(conversations: list[Conversation], page: int, page_size: int, total_items: int) -> ConversationListResponse:
    """Builds a ConversationsResponse object from the given parameters."""

    try:
        total_pages = ceil(total_items / page_size) if total_items else 0
        logger.info("Built conversations response | " "Page: %d | Page Size: %d | " "Total Items: %d | Total Pages: %d", page, page_size, total_items, total_pages)

        return ConversationListResponse(
            conversations=[_conversation_response(conversation) for conversation in conversations],
            page=page,
            page_size=page_size,
            total_items=total_items,
            total_pages=total_pages
        )
    except Exception:
        logger.error("Error building conversations response | " "Page: %d | Page Size: %d", page, page_size, exc_info=True)
        raise

def create_conversation(db: Session, user_id: UUID, data: ConversationCreate) -> ConversationResponse:
    """Creates a new conversation for a user."""

    try:
        conversation = conversation_repo.create(db=db, user_id=user_id, title=data.title)
        db.commit()
        db.refresh(conversation)
        logger.info("Conversation created | " "Conversation ID: %s | User ID: %s", conversation.id, user_id)
        return _conversation_response(conversation)
    except Exception:
        db.rollback()
        logger.error("Error creating conversation | User ID: %s", user_id, exc_info=True)
        raise

def get_conversations(db: Session, user_id: UUID, page: int, page_size: int) -> ConversationListResponse:
    """Retrieves all active conversations for a user."""

    try:
        conversations, total_items = (conversation_repo.get_active_conversations(db=db, user_id=user_id, page=page, page_size=page_size))
        logger.info("Retrieved active conversations | " "User ID: %s | Page: %d | Page Size: %d | Total Items: %d", user_id, page, page_size, total_items)
        return _build_conversations_response(conversations=conversations, page=page, page_size=page_size, total_items=total_items)
    except Exception:
        logger.error("Error retrieving active conversations | User ID: %s", user_id, exc_info=True)
        raise

def get_archived_conversations(db: Session, user_id: UUID, page: int, page_size: int) -> ConversationListResponse:
    """Retrieves all archived conversations for a user."""

    try:
        conversations, total_items = (conversation_repo.get_archived_conversations(db=db, user_id=user_id, page=page, page_size=page_size))
        logger.info("Retrieved archived conversations | " "User ID: %s | Page: %d | Page Size: %d | Total Items: %d", user_id, page, page_size, total_items)
        return _build_conversations_response(conversations=conversations, page=page, page_size=page_size, total_items=total_items)
    except Exception:
        logger.error("Error retrieving archived conversations | User ID: %s", user_id, exc_info=True)
        raise

def get_favorite_conversations(db: Session, user_id: UUID, page: int, page_size: int) -> ConversationListResponse:
    """Retrieves all favorite conversations for a user."""

    try:
        conversations, total_items = (conversation_repo.get_favorite_conversations(db=db, user_id=user_id, page=page, page_size=page_size))
        logger.info("Retrieved favorite conversations | " "User ID: %s | Page: %d | Page Size: %d | Total Items: %d", user_id, page, page_size, total_items)
        return _build_conversations_response(conversations=conversations, page=page, page_size=page_size, total_items=total_items)
    except Exception:
        logger.error("Error retrieving favorite conversations | User ID: %s", user_id, exc_info=True)
        raise

def get_conversation(db: Session, conversation_id: UUID, user_id: UUID) -> ConversationResponse | None:
    """Retrieves a specific conversation owned by a user."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id) 
        if conversation is None:
            logger.warning("Conversation not found | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None
        logger.info("Retrieved conversation | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
        return _conversation_response(conversation)
    except Exception:
        logger.error("Error retrieving conversation | " "Conversation ID: %s | User ID: %s", conversation_id, user_id, exc_info=True)
        raise

def update_conversation(db: Session, conversation_id: UUID, user_id: UUID, data: ConversationUpdate) -> ConversationResponse | None:
    """Updates a conversation owned by a user."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for update | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None
        update_data = data.model_dump(exclude_unset=True, exclude={"id"})
        if not update_data:
            return _conversation_response(conversation)
        conversation = conversation_repo.update(db=db, conversation=conversation, update_data=update_data)
        
        db.commit()
        db.refresh(conversation)
        
        logger.info("Conversation updated | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
        return _conversation_response(conversation)
    except Exception:
        db.rollback()

        logger.error("Error updating conversation | " "Conversation ID: %s | User ID: %s", conversation_id, user_id, exc_info=True)
        raise

def toggle_favorite(db: Session, conversation_id: UUID, user_id: UUID) -> ConversationResponse | None:
    """Toggles the favorite status of a conversation."""

    try:
        conversation = conversation_repo.toggle_favorite(db=db, conversation_id=conversation_id, user_id=user_id) 
        if conversation is None:
            logger.warning("Conversation not found for favorite toggle | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None

        db.commit()
        db.refresh(conversation)

        logger.info("Conversation favorite status toggled | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
        return _conversation_response(conversation)

    except Exception:
        db.rollback()

        logger.error("Error toggling conversation favorite status | " "Conversation ID: %s | User ID: %s", conversation_id, user_id, exc_info=True)
        raise

def toggle_archive(db: Session, conversation_id: UUID, user_id: UUID) -> ConversationResponse | None:
    """Toggles the archived status of a conversation."""

    try:
        conversation = conversation_repo.toggle_archive(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for archive toggle | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None

        db.commit()
        db.refresh(conversation)

        logger.info("Conversation archive status toggled | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
        return _conversation_response(conversation)

    except Exception:
        db.rollback()

        logger.error("Error toggling conversation archive status | " "Conversation ID: %s | User ID: %s", conversation_id, user_id, exc_info=True)
        raise

def delete_conversation(db: Session, conversation_id: UUID, user_id: UUID) -> bool:
    """Deletes a conversation owned by a user."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for deletion | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return False

        conversation_repo.delete(db=db, conversation=conversation)
        db.commit()

        logger.info("Conversation deleted | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
        return True
    except Exception:
        db.rollback()
        logger.error("Error deleting conversation | " "Conversation ID: %s | User ID: %s", conversation_id, user_id, exc_info=True)
        raise