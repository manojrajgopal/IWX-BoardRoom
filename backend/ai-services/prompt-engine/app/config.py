from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="IWX_", env_file=".env", extra="ignore")

    mongo_uri: str = "mongodb://iwx:iwx@mongo:27017"
    mongo_db: str = "iwx_prompts"


settings = Settings()
