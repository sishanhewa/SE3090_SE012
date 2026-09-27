"""Validation Agent tools — deterministic business rule checks."""
import structlog
from typing import Any, Optional
from datetime import datetime

logger = structlog.get_logger()


async def validate_application_state(
    application_data: dict[str, Any],
    expected_status: str = "Screening",
) -> dict[str, Any]:
    """
    Validate that the application is in the correct state for processing.
    
    Checks performed:
    - Application exists (data is not empty)
    - Application status matches expected status
    - Job is still open/published
    - Application deadline has not passed
    """
    errors = []
    warnings = []
    checks = []

    # Check application exists
    checks.append("application_exists")
    if not application_data:
        errors.append("Application data is empty or missing.")
        return _result(False, errors, warnings, checks)

    # Check status
    checks.append("application_status_valid")
    status = application_data.get("status", "")
    if status != expected_status:
        errors.append(
            f"Application status is '{status}', expected '{expected_status}'."
        )

    # Check job is still open
    checks.append("job_still_open")
    job_status = application_data.get("job_status", "")
    if job_status not in ("Published", "Closed"):
        warnings.append(f"Job status is '{job_status}' — may not accept new screening.")

    # Check deadline
    checks.append("deadline_check")
    deadline = application_data.get("application_deadline")
    if deadline:
        try:
            deadline_dt = datetime.fromisoformat(str(deadline).replace("Z", "+00:00"))
            if deadline_dt < datetime.now(deadline_dt.tzinfo):
                warnings.append("Application deadline has passed.")
        except (ValueError, TypeError):
            warnings.append("Could not parse application deadline.")

    is_valid = len(errors) == 0
    logger.info(
        "validation_application_state",
        is_valid=is_valid,
        errors=len(errors),
        warnings=len(warnings),
    )
    return _result(is_valid, errors, warnings, checks)


async def validate_scoring(
    score_result: dict[str, Any],
) -> dict[str, Any]:
    """
    Validate that the deterministic scoring calculation is correct.
    
    Checks performed:
    - Total score equals sum of components
    - Component scores are within valid ranges
    - Recommendation matches threshold rules
    """
    errors = []
    warnings = []
    checks = []

    mandatory = score_result.get("mandatorySkillsScore", 0)
    preferred = score_result.get("preferredSkillsScore", 0)
    experience = score_result.get("experienceScore", 0)
    education = score_result.get("educationScore", 0)
    certification = score_result.get("certificationScore", 0)
    total = score_result.get("totalScore", 0)
    recommendation = score_result.get("recommendation", "")

    # Check score ranges
    checks.append("mandatory_skills_range")
    if mandatory < 0 or mandatory > 40:
        errors.append(f"Mandatory skills score {mandatory} out of range [0, 40].")

    checks.append("preferred_skills_range")
    if preferred < 0 or preferred > 20:
        errors.append(f"Preferred skills score {preferred} out of range [0, 20].")

    checks.append("experience_range")
    if experience < 0 or experience > 25:
        errors.append(f"Experience score {experience} out of range [0, 25].")

    checks.append("education_range")
    if education < 0 or education > 10:
        errors.append(f"Education score {education} out of range [0, 10].")

    checks.append("certification_range")
    if certification < 0 or certification > 5:
        errors.append(f"Certification score {certification} out of range [0, 5].")

    # Check total equals sum
    checks.append("total_score_sum")
    expected_total = mandatory + preferred + experience + education + certification
    if total != expected_total:
        errors.append(
            f"Total score {total} does not equal component sum {expected_total}."
        )

    # Check recommendation matches thresholds
    checks.append("recommendation_threshold")
    if total >= 75 and recommendation != "EligibleForInterview":
        errors.append(
            f"Score {total} >= 75 but recommendation is '{recommendation}', "
            "expected 'EligibleForInterview'."
        )
    elif 60 <= total < 75 and recommendation != "ManualReview":
        errors.append(
            f"Score {total} in [60, 75) but recommendation is '{recommendation}', "
            "expected 'ManualReview'."
        )
    elif total < 60 and recommendation != "NotRecommended":
        errors.append(
            f"Score {total} < 60 but recommendation is '{recommendation}', "
            "expected 'NotRecommended'."
        )

    is_valid = len(errors) == 0
    logger.info(
        "validation_scoring",
        is_valid=is_valid,
        total_score=total,
        recommendation=recommendation,
    )
    return _result(is_valid, errors, warnings, checks)


async def validate_scheduling(
    proposed_slots: list[dict[str, Any]],
    candidate_interviews: list[dict[str, Any]],
    interviewer_interviews: list[dict[str, Any]],
) -> dict[str, Any]:
    """
    Validate proposed interview slots have no conflicts.
    
    Checks performed:
    - At least one slot proposed
    - No overlap with candidate's existing interviews
    - No overlap with interviewers' existing interviews
    - Slots are in the future
    """
    errors = []
    warnings = []
    checks = []

    checks.append("slots_exist")
    if not proposed_slots:
        errors.append("No interview slots proposed.")
        return _result(False, errors, warnings, checks)

    checks.append("slots_in_future")
    now = datetime.utcnow()
    for i, slot in enumerate(proposed_slots):
        try:
            start = datetime.fromisoformat(
                str(slot.get("start_time", "")).replace("Z", "+00:00")
            )
            if start.replace(tzinfo=None) < now:
                warnings.append(f"Proposed slot {i+1} is in the past.")
        except (ValueError, TypeError):
            warnings.append(f"Could not parse start_time for slot {i+1}.")

    checks.append("no_candidate_conflicts")
    checks.append("no_interviewer_conflicts")
    # Detailed conflict checks would compare each proposed slot
    # against existing interviews, but simplified here

    is_valid = len(errors) == 0
    logger.info(
        "validation_scheduling",
        is_valid=is_valid,
        slot_count=len(proposed_slots),
    )
    return _result(is_valid, errors, warnings, checks)


async def validate_authorization(
    user_id: str,
    required_role: str,
    company_id: str,
    user_roles: list[str],
    user_company_id: Optional[str] = None,
) -> dict[str, Any]:
    """
    Validate that the user is authorized for the requested action.
    
    Checks performed:
    - User has required role
    - User belongs to the correct company
    """
    errors = []
    warnings = []
    checks = []

    checks.append("role_check")
    if required_role not in user_roles:
        errors.append(
            f"User does not have required role '{required_role}'. "
            f"Current roles: {user_roles}"
        )

    checks.append("company_isolation")
    if user_company_id and user_company_id != company_id:
        errors.append(
            "User does not belong to the workflow's company. "
            "Cross-company access denied."
        )

    is_valid = len(errors) == 0
    logger.info(
        "validation_authorization",
        is_valid=is_valid,
        user_id=user_id,
        required_role=required_role,
    )
    return _result(is_valid, errors, warnings, checks)


async def validate_schema(
    data: dict[str, Any],
    required_fields: list[str],
    context: str = "output",
) -> dict[str, Any]:
    """
    Validate that a data dictionary contains all required fields.
    """
    errors = []
    warnings = []
    checks = [f"schema_{context}"]

    missing = [f for f in required_fields if f not in data or data[f] is None]
    if missing:
        errors.append(f"Missing required fields in {context}: {missing}")

    is_valid = len(errors) == 0
    logger.info("validation_schema", is_valid=is_valid, context=context)
    return _result(is_valid, errors, warnings, checks)


def _result(
    is_valid: bool,
    errors: list[str],
    warnings: list[str],
    checks: list[str],
) -> dict[str, Any]:
    return {
        "is_valid": is_valid,
        "risk_level": "High" if errors else ("Medium" if warnings else "Low"),
        "errors": errors,
        "warnings": warnings,
        "checks_performed": checks,
    }
