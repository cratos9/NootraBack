from jwt import PyJWTError
import jwt
from core.config import settings

def decode_access_token(token: str) -> dict:
    try:
        payload = jwt.decode(
            token,
            settings.secret_key,
            algorithms=[settings.jwt_algorithm],
            issuer=settings.jwt_issuer,
            audience=settings.jwt_audience,
        )
        return payload
    except PyJWTError:
        raise ValueError("Invalid or expired access token")