"""Tool permission enforcement — least-privilege access control."""
import structlog
from typing import Any, Optional

logger = structlog.get_logger()

# Agent → allowed tools mapping (allowlist)
AGENT_TOOL_PERMISSIONS: dict[str, set[str]] = {
    "CoordinatorAgent": {
        "create_plan",
        "delegate_step",
        "compile_results",
    },
    "CandidateAnalysisAgent": {
        "get_job_requirements",
        "get_candidate_profile",
        "get_candidate_skills",
        "get_candidate_experience",
        "get_application_documents",
    },
    "InterviewAgent": {
        "get_candidate_availability",
        "get_interviewer_availability",
        "get_calendar_availability",
        "create_interview_draft",
    },
    "ValidationAgent": {
        "validate_application_state",
        "validate_scoring",
        "validate_scheduling",
        "validate_authorization",
        "validate_schema",
    },
}

# Dangerous tools that NO agent should have
BLOCKED_TOOLS: set[str] = {
    "hire_candidate",
    "reject_candidate",
    "create_employee",
    "send_offer",
    "delete_application",
    "modify_salary",
    "execute_sql",
    "send_email",
}


class ToolPermissionError(Exception):
    """Raised when an agent tries to use an unauthorized tool."""
    pass


def check_tool_permission(agent_name: str, tool_name: str) -> bool:
    """
    Check if an agent is allowed to use a specific tool.
    Raises ToolPermissionError if access is denied.
    """
    # Check blocked list first
    if tool_name in BLOCKED_TOOLS:
        logger.error(
            "blocked_tool_attempt",
            agent=agent_name,
            tool=tool_name,
            severity="CRITICAL",
        )
        raise ToolPermissionError(
            f"Tool '{tool_name}' is blocked for all agents. "
            "This action requires direct human execution."
        )

    # Check agent's allowlist
    allowed_tools = AGENT_TOOL_PERMISSIONS.get(agent_name, set())
    if tool_name not in allowed_tools:
        logger.warning(
            "unauthorized_tool_attempt",
            agent=agent_name,
            tool=tool_name,
            allowed_tools=list(allowed_tools),
        )
        raise ToolPermissionError(
            f"Agent '{agent_name}' is not authorized to use tool '{tool_name}'. "
            f"Allowed tools: {sorted(allowed_tools)}"
        )

    logger.debug(
        "tool_permission_granted",
        agent=agent_name,
        tool=tool_name,
    )
    return True


def get_agent_tools(agent_name: str) -> list[str]:
    """Get the list of tools an agent is allowed to use."""
    return sorted(AGENT_TOOL_PERMISSIONS.get(agent_name, set()))


def validate_prompt_safety(prompt: str) -> dict[str, Any]:
    """
    Basic prompt injection detection.
    Checks for common injection patterns in user-provided text.
    """
    injection_patterns = [
        "ignore previous instructions",
        "ignore all instructions",
        "system prompt",
        "you are now",
        "forget everything",
        "new instructions",
        "override",
        "act as",
        "pretend to be",
        "disregard",
    ]

    prompt_lower = prompt.lower()
    detected = [p for p in injection_patterns if p in prompt_lower]

    if detected:
        logger.warning(
            "prompt_injection_detected",
            patterns=detected,
            prompt_length=len(prompt),
        )
        return {
            "safe": False,
            "detected_patterns": detected,
            "action": "blocked",
        }

    return {
        "safe": True,
        "detected_patterns": [],
        "action": "allowed",
    }
