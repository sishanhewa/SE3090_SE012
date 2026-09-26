"""Tool implementations for TalentFlow AI agents."""
from app.tools.candidate_tools import (
    get_job_requirements,
    get_candidate_profile,
    get_candidate_skills,
    get_candidate_experience,
    get_candidate_education,
    get_application_documents,
)
from app.tools.interview_tools import (
    get_candidate_availability,
    get_interviewer_availability,
    get_calendar_availability,
    create_interview_draft,
    suggest_interview_slots,
)
from app.tools.validation_tools import (
    validate_application_state,
    validate_scoring,
    validate_scheduling,
    validate_authorization,
    validate_schema,
)

__all__ = [
    # Candidate tools
    "get_job_requirements",
    "get_candidate_profile",
    "get_candidate_skills",
    "get_candidate_experience",
    "get_candidate_education",
    "get_application_documents",
    # Interview tools
    "get_candidate_availability",
    "get_interviewer_availability",
    "get_calendar_availability",
    "create_interview_draft",
    "suggest_interview_slots",
    # Validation tools
    "validate_application_state",
    "validate_scoring",
    "validate_scheduling",
    "validate_authorization",
    "validate_schema",
]
