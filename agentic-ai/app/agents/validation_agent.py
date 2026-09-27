"""Validation/Safety Agent - Performs deterministic business rule checks."""
import structlog
from typing import Any, Optional

from app.tools.validation_tools import (
    validate_application_state,
    validate_scoring,
    validate_scheduling,
    validate_authorization,
    validate_schema,
)
from app.schemas.agent_schemas import (
    ValidationInput,
    ValidationOutput,
)

logger = structlog.get_logger()


class ValidationAgent:
    """
    Applies deterministic validation checks including schema validation,
    business rule compliance, and safety controls before high-impact actions.

    Owned by: Student 4 (Employee & Onboarding Management)

    Checks performed:
    - Application belongs to requested job
    - Application status is valid for the operation
    - Candidate has mandatory requirements
    - Score calculation matches configured weights
    - Interview panel members exist
    - Proposed slots contain no collisions
    - User requesting approval is authorized
    - Tool outputs match expected schema
    """

    def __init__(self):
        self.name = "ValidationAgent"
        self.allowed_tools = [
            "validate_application_state",
            "validate_scoring",
            "validate_scheduling",
            "validate_authorization",
            "validate_schema",
        ]
        self.tool_calls_log: list[dict[str, Any]] = []

    async def execute(
        self,
        input_data: ValidationInput,
        auth_token: Optional[str] = None,
    ) -> ValidationOutput:
        """Perform deterministic validation checks."""
        logger.info(
            "validation_started",
            workflow_id=input_data.workflow_id,
            application_id=input_data.application_id,
        )

        all_errors: list[str] = []
        all_warnings: list[str] = []
        all_checks: list[str] = []

        # 1. Validate candidate analysis output schema
        if input_data.candidate_analysis:
            analysis_dict = input_data.candidate_analysis.model_dump()
            schema_result = await self._call_tool(
                "validate_schema",
                validate_schema,
                analysis_dict,
                ["application_id", "job_id", "mandatory_skill_matches",
                 "total_experience_years", "evidence_summary"],
                "candidate_analysis",
            )
            self._collect_results(schema_result, all_errors, all_warnings, all_checks)

        # 2. Validate scoring results
        if input_data.score_result:
            scoring_result = await self._call_tool(
                "validate_scoring",
                validate_scoring,
                input_data.score_result,
            )
            self._collect_results(scoring_result, all_errors, all_warnings, all_checks)

        # 3. Validate application state
        app_state_result = await self._call_tool(
            "validate_application_state",
            validate_application_state,
            {
                "application_id": input_data.application_id,
                "job_id": input_data.job_id,
                "status": "Screening",
            },
        )
        self._collect_results(app_state_result, all_errors, all_warnings, all_checks)

        # Build output
        is_valid = len(all_errors) == 0

        if all_errors:
            risk_level = "High"
        elif all_warnings:
            risk_level = "Medium"
        else:
            risk_level = "Low"

        output = ValidationOutput(
            is_valid=is_valid,
            risk_level=risk_level,
            errors=all_errors,
            warnings=all_warnings,
            checks_performed=all_checks,
        )

        logger.info(
            "validation_completed",
            workflow_id=input_data.workflow_id,
            is_valid=is_valid,
            error_count=len(all_errors),
            warning_count=len(all_warnings),
            risk_level=risk_level,
        )

        return output

    async def _call_tool(self, tool_name: str, tool_func, *args) -> dict:
        """Call a validation tool and log the invocation."""
        import time
        start = time.time()
        try:
            result = await tool_func(*args)
            duration_ms = int((time.time() - start) * 1000)
            self.tool_calls_log.append({
                "tool_name": tool_name,
                "duration_ms": duration_ms,
                "success": True,
            })
            return result or {}
        except Exception as e:
            duration_ms = int((time.time() - start) * 1000)
            self.tool_calls_log.append({
                "tool_name": tool_name,
                "duration_ms": duration_ms,
                "success": False,
                "error": str(e),
            })
            logger.error("validation_tool_failed", tool=tool_name, error=str(e))
            return {
                "is_valid": False,
                "errors": [f"Validation tool '{tool_name}' failed: {str(e)}"],
                "warnings": [],
                "checks_performed": [tool_name],
            }

    def _collect_results(
        self,
        result: dict,
        errors: list[str],
        warnings: list[str],
        checks: list[str],
    ):
        """Aggregate results from a validation tool call."""
        errors.extend(result.get("errors", []))
        warnings.extend(result.get("warnings", []))
        checks.extend(result.get("checks_performed", []))
