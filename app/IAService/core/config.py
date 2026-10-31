from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    app_name: str
    debug: bool
    port: int
    database_url: str
    redis_url: str
    redis_cache_expire: int
    secret_key: str
    jwt_algorithm: str
    jwt_issuer: str
    jwt_audience: str
    encryption_key: str
    api_ia_url: str
    api_ia_model: str
    api_ia_key: str
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", case_sensitive=False)
    
settings = Settings()