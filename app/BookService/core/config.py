from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    app_name: str
    debug: bool
    port: int
    database_url: str
    redis_url: str
    secret_key: str
    jwt_algorithm: str
    jwt_issuer: str
    jwt_audience: str
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", case_sensitive=False)
    
settings = Settings()