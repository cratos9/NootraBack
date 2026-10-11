from fastapi import FastAPI
from api.routes.books import router as books_router
from slowapi.errors import RateLimitExceeded
from slowapi import _rate_limit_exceeded_handler
from core.rate_limit import limiter

app = FastAPI()


app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)

app.include_router(books_router)

@app.get("/")
async def root():
    return {"message": "Hello World"}