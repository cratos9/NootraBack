import logging
import redis
from redis.exceptions import RedisError
from core.config import settings

logger = logging.getLogger(__name__)

redis_client = redis.Redis.from_url(settings.redis_url, decode_responses=True, socket_connect_timeout=2, socket_timeout=2, health_check_interval=30)

def check_redis_connection() -> None:
    """Check the Redis connection and log the status."""
    try:
        return bool(redis_client.ping())
    except RedisError:
        logger.error("Failed to connect to Redis server")
        return False