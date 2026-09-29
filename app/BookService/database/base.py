from sqlalchemy import create_engine
from core.config import settings
from sqlalchemy.orm import DeclarativeBase

class Base(DeclarativeBase):
    pass

engine = create_engine(settings.database_url, echo=True)