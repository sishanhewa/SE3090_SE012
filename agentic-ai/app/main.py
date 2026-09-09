"""TalentFlow AI - Agentic AI Service"""
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

app = FastAPI(
    title="TalentFlow AI - Agentic Service",
    description="AI-powered recruitment screening and candidate assessment service",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Restrict in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/health")
async def health_check():
    return {"status": "healthy", "service": "agentic-ai"}


@app.get("/")
async def root():
    return {"message": "TalentFlow AI - Agentic Service", "version": "1.0.0"}
