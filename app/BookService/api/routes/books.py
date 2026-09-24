from fastapi import APIRouter, Depends, status

router = APIRouter(prefix="/api/v1/books", tags=["Books"])

@router.get("/")
async def get_books():
    return {"message": "List of books"}