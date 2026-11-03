import time
from uuid import UUID
from sqlalchemy.orm import Session
from models.message import Message
from repositories import message as message_repo
from repositories import conversation as conversation_repo
from schemas.message import MessageUpdate, MessageResponse, MessageListResponse
from integrations.ia_provider.client import IA
import logging

ia = IA()

logger = logging.getLogger(__name__)

def _message_response(message: Message) -> MessageResponse:
    """Builds a MessageResponse object."""

    try:
        return MessageResponse.model_validate(message)
    except Exception:
        logger.error("Error building message response | Message ID: %s", message.id, exc_info=True)
        raise

def _build_messages_response(messages: list[Message]) -> MessageListResponse:
    """Builds a MessageListResponse object."""

    try:
        logger.info("Built messages response | Total Messages: %d", len(messages))
        return MessageListResponse(messages=[_message_response(message) for message in messages])
    except Exception:
        logger.error("Error building messages response", exc_info=True)
        raise

def _update_conversation_statistics(conversation, messages: list[Message]) -> None:
    """Recalculates conversation statistics from its messages."""

    conversation.message_count = len(messages)
    conversation.total_tokens = sum((message.query_tokens or 0) + (message.response_tokens or 0) for message in messages)
    conversation.total_response_time = sum(message.response_time_ms or 0 for message in messages)
    conversation.average_response_time = (conversation.total_response_time / conversation.message_count if conversation.message_count else 0.0)

def get_messages(db: Session, conversation_id: UUID, user_id: UUID) -> MessageListResponse | None:
    """Retrieves all messages from a conversation owned by a user."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for retrieving messages | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None
        messages = message_repo.get_messages(db=db, conversation_id=conversation_id)
        logger.info("Retrieved conversation messages | " "Conversation ID: %s | User ID: %s | Total Messages: %d", conversation_id, user_id, len(messages))

        return _build_messages_response(messages)

    except Exception:
        logger.error("Error retrieving conversation messages | " "Conversation ID: %s | User ID: %s", conversation_id, user_id, exc_info=True)
        raise

def get_message(db: Session, message_id: UUID, conversation_id: UUID, user_id: UUID) -> MessageResponse | None:
    """Retrieves a specific message from a user's conversation."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for retrieving message | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None

        message = message_repo.get_by_id(db=db, message_id=message_id, conversation_id=conversation_id)

        if message is None:
            logger.warning("Message not found | Message ID: %s | Conversation ID: %s", message_id, conversation_id)
            return None

        logger.info("Retrieved message | Message ID: %s | User ID: %s", message_id, user_id)
        return _message_response(message)

    except Exception:
        logger.error("Error retrieving message | Message ID: %s | User ID: %s", message_id, user_id, exc_info=True)
        raise

async def ask_ai(db: Session, conversation_id: UUID, user_id: UUID, query_text: str) -> MessageResponse | None:
    """Sends a question to the AI and stores the conversation message."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for AI request | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None

        previous_messages = message_repo.get_messages(db=db, conversation_id=conversation_id)
        history = []

        for message in previous_messages:
            history.append({
                "role": "user",
                "content": message.query_text,
            })

            history.append({
                "role": "assistant",
                "content": message.response_text,
            })

        start_time = time.perf_counter()
        ai_response = await ia.ask(prompt=query_text, memory_messages=history)
        response_time_ms = int((time.perf_counter() - start_time) * 1000)

        query_tokens = ai_response.get("query_tokens") or 0
        response_tokens = ai_response.get("response_tokens") or 0

        message_data = {
            "conversation_id": conversation_id,
            "order": conversation.message_count + 1,
            "query_text": query_text,
            "response_text": ai_response.get("text") or "",
            "query_type": "general",
            "query_tokens": query_tokens,
            "response_time_ms": response_time_ms,
            "response_tokens": response_tokens,
            "language": "es",
        }

        message = message_repo.create(db=db, message_data=message_data)
        conversation.message_count += 1
        conversation.total_tokens += (query_tokens + response_tokens)
        conversation.total_response_time += response_time_ms
        conversation.average_response_time = (conversation.total_response_time / conversation.message_count if conversation.message_count else 0.0)

        db.commit()
        db.refresh(message)

        logger.info("AI message created | " "Message ID: %s | Conversation ID: %s | User ID: %s", message.id, conversation_id, user_id)
        return _message_response(message)

    except Exception:
        db.rollback()
        logger.error("Error creating AI message | " "Conversation ID: %s | User ID: %s", conversation_id, user_id, exc_info=True)
        raise

def update_message(db: Session, message_id: UUID, conversation_id: UUID, user_id: UUID, data: MessageUpdate) -> MessageResponse | None:
    """Updates a message owned by a user."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for message update | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return None

        message = message_repo.get_by_id(db=db, message_id=message_id, conversation_id=conversation_id)
        if message is None:
            logger.warning("Message not found for update | " "Message ID: %s | Conversation ID: %s", message_id, conversation_id)
            return None

        update_data = data.model_dump(exclude_unset=True, exclude={"id", "conversation_id"})
        if not update_data:
            return _message_response(message)

        message = message_repo.update(db=db, message=message, update_data=update_data)

        db.commit()
        db.refresh(message)

        logger.info("Message updated | " "Message ID: %s | Conversation ID: %s | User ID: %s", message_id, conversation_id, user_id)
        return _message_response(message)

    except Exception:
        db.rollback()

        logger.error("Error updating message | " "Message ID: %s | Conversation ID: %s | User ID: %s", message_id, conversation_id, user_id, exc_info=True)
        raise

def delete_message(db: Session, message_id: UUID, conversation_id: UUID, user_id: UUID) -> bool:
    """Deletes a message and recalculates conversation statistics."""

    try:
        conversation = conversation_repo.get_by_id(db=db, conversation_id=conversation_id, user_id=user_id)
        if conversation is None:
            logger.warning("Conversation not found for message deletion | " "Conversation ID: %s | User ID: %s", conversation_id, user_id)
            return False

        message = message_repo.get_by_id(db=db, message_id=message_id, conversation_id=conversation_id)
        if message is None:
            logger.warning("Message not found for deletion | " "Message ID: %s | Conversation ID: %s", message_id, conversation_id)
            return False

        message_repo.delete(db=db, message=message)
        remaining_messages = message_repo.get_messages(db=db, conversation_id=conversation_id)
        for index, remaining_message in enumerate(remaining_messages, start=1):
            remaining_message.order = index

        _update_conversation_statistics(conversation=conversation, messages=remaining_messages)

        db.commit()

        logger.info("Message deleted | " "Message ID: %s | Conversation ID: %s | User ID: %s", message_id, conversation_id, user_id)
        return True

    except Exception:
        db.rollback()

        logger.error("Error deleting message | " "Message ID: %s | Conversation ID: %s | User ID: %s", message_id, conversation_id, user_id, exc_info=True)
        raise