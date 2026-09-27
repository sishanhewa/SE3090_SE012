"""Gemini API client for structured generation via LangChain."""
import os
import structlog
from typing import Any, Optional
from langchain_google_genai import ChatGoogleGenerativeAI
from pydantic import BaseModel

logger = structlog.get_logger()


class GeminiClient:
    """
    Wrapper around the Gemini API using LangChain's ChatGoogleGenerativeAI.
    Provides structured output generation for the agent workflow.
    """

    def __init__(self, model_name: str = "gemini-2.0-flash"):
        api_key = os.getenv("GOOGLE_API_KEY", "")
        if not api_key:
            logger.warning("GOOGLE_API_KEY not set — Gemini calls will fail")

        self.llm = ChatGoogleGenerativeAI(
            model=model_name,
            google_api_key=api_key,
            temperature=0.1,
            max_output_tokens=4096,
        )
        self.model_name = model_name
        logger.info("gemini_client_initialized", model=model_name)

    async def generate_text(
        self,
        prompt: str,
        system_instruction: Optional[str] = None,
    ) -> str:
        """Generate unstructured text from a prompt."""
        messages = []
        if system_instruction:
            messages.append(("system", system_instruction))
        messages.append(("human", prompt))

        response = await self.llm.ainvoke(messages)
        logger.info(
            "gemini_text_generated",
            prompt_length=len(prompt),
            response_length=len(response.content),
        )
        return response.content

    async def generate_structured(
        self,
        prompt: str,
        output_schema: type[BaseModel],
        system_instruction: Optional[str] = None,
    ) -> BaseModel:
        """Generate structured output conforming to a Pydantic schema."""
        structured_llm = self.llm.with_structured_output(output_schema)

        messages = []
        if system_instruction:
            messages.append(("system", system_instruction))
        messages.append(("human", prompt))

        result = await structured_llm.ainvoke(messages)
        logger.info(
            "gemini_structured_generated",
            schema=output_schema.__name__,
            prompt_length=len(prompt),
        )
        return result

    async def analyze_candidate(
        self,
        job_requirements: dict[str, Any],
        candidate_data: dict[str, Any],
    ) -> dict[str, Any]:
        """
        Use Gemini to analyze a candidate against job requirements.
        Returns structured analysis (skills matches, experience assessment).
        """
        prompt = f"""Analyze this candidate against the job requirements.

JOB REQUIREMENTS:
{_format_dict(job_requirements)}

CANDIDATE DATA:
{_format_dict(candidate_data)}

Provide a structured analysis with:
1. Which mandatory skills the candidate has vs. is missing
2. Which preferred skills the candidate has vs. is missing
3. Experience relevance assessment
4. Education relevance assessment
5. Overall qualitative assessment
6. Any concerns or red flags

Important: Do NOT provide a numerical score. Scoring is handled
by the deterministic scoring service. Focus on qualitative analysis only."""

        system_instruction = (
            "You are a recruitment screening assistant. "
            "Analyze candidates objectively based on evidence in their profile. "
            "Be factual and cite specific data points from the candidate's profile."
        )

        response = await self.generate_text(prompt, system_instruction)
        return {
            "analysis": response,
            "model": self.model_name,
        }


def _format_dict(d: dict[str, Any], indent: int = 0) -> str:
    """Format a dict for prompt inclusion."""
    lines = []
    prefix = "  " * indent
    for key, value in d.items():
        if isinstance(value, dict):
            lines.append(f"{prefix}{key}:")
            lines.append(_format_dict(value, indent + 1))
        elif isinstance(value, list):
            lines.append(f"{prefix}{key}:")
            for item in value:
                if isinstance(item, dict):
                    lines.append(_format_dict(item, indent + 1))
                else:
                    lines.append(f"{prefix}  - {item}")
        else:
            lines.append(f"{prefix}{key}: {value}")
    return "\n".join(lines)
