from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    service_name: str = "orchestrator-ai"
    host: str = "0.0.0.0"
    port: int = 8000

    rabbitmq_host: str = "rabbitmq"
    rabbitmq_port: int = 5672
    rabbitmq_user: str = "iwx"
    rabbitmq_pass: str = "iwx"

    queue_task_approved: str = "iwx.task.approved"
    queue_task_completed: str = "iwx.task.completed"
    queue_agent_thinking: str = "iwx.agent.thinking"

    ollama_host: str = "http://ollama:11434"
    ollama_model: str = "llama3.2:3b"

    class Config:
        env_prefix = "IWX_"
        env_file = ".env"


settings = Settings()
