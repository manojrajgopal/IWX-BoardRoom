from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="IWX_", env_file=".env", extra="ignore")
    llm_router_url: str = "http://llm-router:8101"
    prompt_engine_url: str = "http://prompt-engine:8102"
    memory_engine_url: str = "http://memory-engine:8100"
    rag_engine_url: str = "http://rag-engine:8104"
    default_provider: str = "ollama"
    default_model: str = "llama3.2:3b"
    request_timeout_seconds: float = 180.0


settings = Settings()
