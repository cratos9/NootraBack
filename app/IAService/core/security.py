from jwt import PyJWTError
import jwt
from core.config import settings
from cryptography.fernet import Fernet

cipher = Fernet(settings.encryption_key.encode())

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
    
def encrypt_data(data: str) -> str:
    """ Encrypts the given data using Fernet symmetric encryption."""
    encrypted_data = cipher.encrypt(data.encode('utf-8'))
    return encrypted_data.decode('utf-8')

def decrypt_data(encrypted_data: str) -> str:
    """ Decrypts the given encrypted data using Fernet symmetric encryption."""
    decrypted_data = cipher.decrypt(encrypted_data.encode('utf-8'))
    return decrypted_data.decode('utf-8')