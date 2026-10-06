"""Resolve the .NET API URL for local runs and Vercel service bindings."""
import os


def backend_api_url() -> str:
    """The bound service URL is a root; its API routes live under /api."""
    bound_url = os.getenv("BACKEND_SERVICE_URL")
    if bound_url:
        return f"{bound_url.rstrip('/')}/api"
    return os.getenv("BACKEND_URL", "http://localhost:5155/api").rstrip("/")
