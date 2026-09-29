from fastapi import FastAPI
from api.routes.books import router as books_router

app = FastAPI()

app.include_router(books_router)

@app.get("/")
async def root():
    return {"message": "Hello World"}