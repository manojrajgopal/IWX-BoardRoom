from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="IWX_", env_file=".env", extra="ignore")
    model_name: str = "sentence-transformers/all-MiniLM-L6-v2"
    device: str = "cpu"


settings = Settings()
