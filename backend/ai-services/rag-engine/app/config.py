from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="IWX_", env_file=".env", extra="ignore")
    vector_engine_url: str = "http://vector-engine:8103"
    chroma_path: str = "/data/chroma"
    request_timeout_seconds: float = 60.0


settings = Settings()
