"""Gemini API client for structured generation via LangChain."""
import os
import structlog
from typing import Any, Optional
from langchain_google_genai import ChatGoogleGenerativeAI
from pydantic import BaseModel
from dotenv import load_dotenv

load_dotenv()

logger = structlog.get_logger()


class GeminiClient:
    """
    Wrapper around the Gemini API using LangChain's ChatGoogleGenerativeAI.
    Supports automatic key fallback when the primary key is rate-limited.
    """

    def __init__(self, model_name: str = "gemini-3.8-flash"):
        self._api_keys = [v for k, v in os.environ.items() if k.startswith("GOOGLE_API_KEY") and v]

        if not self._api_keys:
            logger.warning("No GOOGLE_API_KEY set — Gemini calls will fail")
            self._api_keys.append("")  # placeholder so __init__ doesn't crash

        self._current_key_index = 0
        self.model_name = model_name
        self.llm = self._build_llm(self._api_keys[0])
        logger.info(
            "gemini_client_initialized",
            model=model_name,
            keys_available=len(self._api_keys),
        )

    def _build_llm(self, api_key: str) -> ChatGoogleGenerativeAI:
        return ChatGoogleGenerativeAI(
            model=self.model_name,
            google_api_key=api_key,
            temperature=0.1,
            max_output_tokens=4096,
        )

    def _rotate_key(self) -> bool:
        """Switch to the next available API key. Returns False if none left."""
        next_index = self._current_key_index + 1
        if next_index < len(self._api_keys):
            self._current_key_index = next_index
            self.llm = self._build_llm(self._api_keys[next_index])
            logger.info("gemini_key_rotated", key_index=next_index)
            return True
        logger.error("gemini_all_keys_exhausted")
        return False

    async def _invoke_with_fallback(self, messages: list, output_schema: Optional[type[BaseModel]] = None):
        """Invoke LLM with automatic key rotation on rate-limit errors."""
        llm = self.llm.with_structured_output(output_schema) if output_schema else self.llm
        try:
            return await llm.ainvoke(messages)
        except Exception as e:
            error_str = str(e).lower()
            is_retryable = any(
                kw in error_str
                for kw in ["429", "rate", "quota", "resource_exhausted", "limit"]
            )
            if is_retryable and self._rotate_key():
                logger.warning("gemini_retrying_with_fallback_key", error=str(e))
                new_llm = self.llm.with_structured_output(output_schema) if output_schema else self.llm
                return await new_llm.ainvoke(messages)
            raise

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

        response = await self._invoke_with_fallback(messages)
        content = response.content
        if isinstance(content, list):
            text_content = ""
            for part in content:
                if isinstance(part, dict) and part.get("type") == "text":
                    text_content += part.get("text", "")
                elif isinstance(part, str):
                    text_content += part
            content = text_content

        logger.info(
            "gemini_text_generated",
            prompt_length=len(prompt),
            response_length=len(content),
        )
        return content

    async def generate_structured(
        self,
        prompt: str,
        output_schema: type[BaseModel],
        system_instruction: Optional[str] = None,
    ) -> BaseModel:
        """Generate structured output conforming to a Pydantic schema."""
        messages = []
        if system_instruction:
            messages.append(("system", system_instruction))
        messages.append(("human", prompt))

        result = await self._invoke_with_fallback(messages, output_schema)
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

