"""Structured logging configuration for the Agentic AI service."""
import structlog
import logging
from datetime import datetime


def configure_logging(log_level: str = "INFO") -> None:
    """Configure structured logging with JSON output."""
    structlog.configure(
        processors=[
            structlog.contextvars.merge_contextvars,
            structlog.processors.add_log_level,
            structlog.processors.StackInfoRenderer(),
            structlog.dev.set_exc_info,
            structlog.processors.TimeStamper(fmt="iso"),
            structlog.processors.JSONRenderer(),
        ],
        wrapper_class=structlog.make_filtering_bound_logger(
            getattr(logging, log_level.upper(), logging.INFO)
        ),
        context_class=dict,
        logger_factory=structlog.PrintLoggerFactory(),
        cache_logger_on_first_use=True,
    )


class WorkflowLogger:
    """Specialized logger for workflow execution events."""

    def __init__(self, workflow_id: str):
        self.logger = structlog.get_logger()
        self.workflow_id = workflow_id
        self.events: list[dict] = []

    def log_step_start(self, agent_name: str, step_number: int):
        event = {
            "event": "step_start",
            "workflow_id": self.workflow_id,
            "agent": agent_name,
            "step": step_number,
            "timestamp": datetime.utcnow().isoformat(),
        }
        self.events.append(event)
        self.logger.info("workflow_step_started", **event)

    def log_step_complete(self, agent_name: str, step_number: int, duration_ms: int):
        event = {
            "event": "step_complete",
            "workflow_id": self.workflow_id,
            "agent": agent_name,
            "step": step_number,
            "duration_ms": duration_ms,
            "timestamp": datetime.utcnow().isoformat(),
        }
        self.events.append(event)
        self.logger.info("workflow_step_completed", **event)

    def log_step_error(self, agent_name: str, step_number: int, error: str):
        event = {
            "event": "step_error",
            "workflow_id": self.workflow_id,
            "agent": agent_name,
            "step": step_number,
            "error": error,
            "timestamp": datetime.utcnow().isoformat(),
        }
        self.events.append(event)
        self.logger.error("workflow_step_failed", **event)

    def log_tool_call(self, tool_name: str, duration_ms: int, success: bool):
        event = {
            "event": "tool_call",
            "workflow_id": self.workflow_id,
            "tool": tool_name,
            "duration_ms": duration_ms,
            "success": success,
            "timestamp": datetime.utcnow().isoformat(),
        }
        self.events.append(event)
        self.logger.info("workflow_tool_called", **event)

    def get_execution_log(self) -> list[dict]:
        """Return the complete execution log for persistence."""
        return self.events
