import logging
from time import perf_counter
from uuid import uuid4
from http import HTTPStatus

from fastapi import FastAPI, Request, status
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from fastapi import HTTPException
from api.routes.books import router as books_router
from api.routes.note import router as notes_router
from api.routes.attachment import router as attachment_router
from slowapi.errors import RateLimitExceeded
from core.rate_limit import limiter
from core.logging import configure_logging
from contextlib import asynccontextmanager
from database.redis import check_redis_connection, redis_client
from core.errors import ApiError, problem_payload, validation_error

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
app = FastAPI(lifespan=lifespan, title="Book Service", description="A service for managing books, notes and archives.", version="1.1.0")


app.state.limiter = limiter


def _trace_id(request: Request) -> str:
    return getattr(request.state, "request_id", request.headers.get("X-Request-ID", str(uuid4())))


@app.exception_handler(ApiError)
async def api_error_handler(request: Request, exc: ApiError) -> JSONResponse:
    trace_id = _trace_id(request)
    return JSONResponse(
        status_code=exc.status_code,
        content=problem_payload(exc, trace_id, str(request.url.path)),
        headers={"X-Request-ID": trace_id},
    )


@app.exception_handler(HTTPException)
async def http_exception_handler(request: Request, exc: HTTPException) -> JSONResponse:
    detail = exc.detail if isinstance(exc.detail, str) else "The request could not be processed."
    error = ApiError(
        status_code=exc.status_code,
        code=f"HTTP_{exc.status_code}",
        title=HTTPStatus._value2member_map_.get(exc.status_code, HTTPStatus.INTERNAL_SERVER_ERROR).phrase,
        detail=detail,
        error_type=f"https://api.nootra.com/problems/http-{exc.status_code}",
    )
    headers = dict(exc.headers or {})
    trace_id = _trace_id(request)
    headers["X-Request-ID"] = trace_id
    return JSONResponse(
        status_code=exc.status_code,
        content=problem_payload(error, trace_id, str(request.url.path)),
        headers=headers,
    )


@app.exception_handler(RequestValidationError)
async def request_validation_error_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    fields: dict[str, list[str]] = {}
    for error in exc.errors():
        location = [str(part) for part in error["loc"] if part != "body"]
        field = ".".join(location) or "request"
        fields.setdefault(field, []).append(error["msg"])
    api_error = validation_error(fields)
    trace_id = _trace_id(request)
    return JSONResponse(
        status_code=api_error.status_code,
        content=problem_payload(api_error, trace_id, str(request.url.path)),
        headers={"X-Request-ID": trace_id},
    )


@app.exception_handler(RateLimitExceeded)
async def rate_limit_handler(request: Request, exc: RateLimitExceeded) -> JSONResponse:
    trace_id = _trace_id(request)
    error = ApiError(
        status_code=status.HTTP_429_TOO_MANY_REQUESTS,
        code="RATE_LIMIT_EXCEEDED",
        title="Too many requests",
        detail="Please try again later.",
        error_type="https://api.nootra.com/problems/rate-limit-exceeded",
    )
    return JSONResponse(
        status_code=error.status_code,
        content=problem_payload(error, trace_id, str(request.url.path)) | {"retryAfterSeconds": 60},
        headers={"Retry-After": "60", "X-Request-ID": trace_id},
    )


@app.exception_handler(Exception)
async def unhandled_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    trace_id = _trace_id(request)
    logger.exception(
        "Unhandled exception | Request ID: %s | Path: %s | Method: %s",
        trace_id,
        request.url.path,
        request.method,
    )
    error = ApiError(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        code="INTERNAL_ERROR",
        title="Internal server error",
        detail="An unexpected error occurred.",
        error_type="https://api.nootra.com/problems/internal-error",
    )
    return JSONResponse(
        status_code=error.status_code,
        content=problem_payload(error, trace_id, str(request.url.path)),
        headers={"X-Request-ID": trace_id},
    )

app.include_router(books_router)
app.include_router(notes_router)
app.include_router(attachment_router)

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
    finally:
        duration_ms = (perf_counter() - start_time) * 1000
        logger.info("Request completed | Request ID: %s | Path: %s | Method: %s | Status Code: %d | Duration: %.2f ms", request_id, request.url.path, request.method, status_code, duration_ms,)