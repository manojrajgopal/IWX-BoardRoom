from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="IWX_", env_file=".env", extra="ignore")

    ollama_host: str = "http://ollama:11434"
    ollama_default_model: str = "llama3.2:3b"
    openai_api_key: str = ""
    openai_base_url: str = "https://api.openai.com/v1"
    openai_default_model: str = "gpt-4o-mini"
    request_timeout_seconds: float = 120.0


settings = Settings()
