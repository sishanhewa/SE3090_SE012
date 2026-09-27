import os
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    # App Settings
    app_name: str = "TalentFlow Agentic AI Service"
    debug: bool = True
    
    # API Keys
    gemini_api_key: str = os.getenv("GEMINI_API_KEY", "dummy-key-for-local-dev")
    
    # Backend Integration
    backend_url: str = os.getenv("BACKEND_URL", "http://localhost:5000/api")
    
    # Workflow Settings
    max_steps_per_workflow: int = 15
    enable_human_approval: bool = True

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"

settings = Settings()
