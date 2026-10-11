import logging
from time import perf_counter
from uuid import uuid4

from fastapi import FastAPI, Request
from api.routes.books import router as books_router
from slowapi.errors import RateLimitExceeded
from slowapi import _rate_limit_exceeded_handler
from core.rate_limit import limiter
from core.logging import configure_logging
from contextlib import asynccontextmanager
from database.redis import check_redis_connection, redis_client

@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.redis = redis_client
    
    if check_redis_connection():
        logging.info("Successfully connected to Redis.")
    else:
        logging.warning("Failed to connect to Redis. Some features may not work as expected.")
    
    try:
        yield
    finally:
        redis_client.close()
        logging.info("Redis connection closed.")

configure_logging()

logger = logging.getLogger(__name__)
app = FastAPI(lifespan=lifespan, title="Book Service", description="A service for managing books.", version="1.0.0")


app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)

app.include_router(books_router)

@app.middleware("http")
async def log_request(request: Request, call_next):
    request_id = str(uuid4())
    request.state.request_id = request_id
    start_time = perf_counter()
    status_code = 500
    try:
        response = await call_next(request)
        status_code = response.status_code
        response.headers["X-Request-ID"] = request_id
        return response
    except Exception:
        logger.exception("Unexpected error occurred while processing request. | Request ID: %s | Path: %s | Method: %s", request_id, request.url.path, request.method) 
        raise
    finally:
        duration_ms = (perf_counter() - start_time) * 1000
        logger.info("Request completed | Request ID: %s | Path: %s | Method: %s | Status Code: %d | Duration: %.2f ms", request_id, request.url.path, request.method, status_code, duration_ms,)

@app.get("/")
async def root():
    return {"message": "Hello World"}