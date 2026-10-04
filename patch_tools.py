import re

with open('agentic-ai/app/tools/interview_tools.py', 'r') as f:
    content = f.read()

pattern = r'async def get_candidate_availability\(.*?\) -> dict\[str, Any\]:\n    """\n    Check candidate interview availability within a date range.\n    Returns existing interviews that would cause conflicts.\n    \n    Allowed tool for: InterviewAgent\n    """\n.*?(?=async def get_interviewer_availability)'

replacement = '''async def get_candidate_availability(
    candidate_profile_id: str,
    date_range_start: str,
    date_range_end: str,
    auth_token: str | None = None,
) -> dict[str, Any]:
    """
    Check candidate interview availability within a date range.
    Returns existing interviews that would cause conflicts.
    
    Allowed tool for: InterviewAgent
    """
    logger.info(
        "tool_get_candidate_availability",
        candidate_profile_id=candidate_profile_id,
        note="Backend endpoint pending - returning no conflicts",
    )
    return {
        "candidate_profile_id": candidate_profile_id,
        "date_range": {"start": date_range_start, "end": date_range_end},
        "existing_interviews": [],
        "has_conflicts": False,
        "note": "Availability check mocked.",
    }


'''

new_content = re.sub(pattern, replacement, content, flags=re.DOTALL)

with open('agentic-ai/app/tools/interview_tools.py', 'w') as f:
    f.write(new_content)
