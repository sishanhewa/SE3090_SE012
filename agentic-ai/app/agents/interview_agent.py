"""Interview Scheduling Agent - Proposes interview slots based on availability."""
import structlog
from typing import Any, Optional
from datetime import datetime, timedelta

from app.tools.interview_tools import (
    get_candidate_availability,
    get_interviewer_availability,
    get_calendar_availability,
    create_interview_draft,
    suggest_interview_slots,
)
from app.schemas.agent_schemas import (
    InterviewSchedulingInput,
    InterviewSchedulingOutput,
    ProposedSlot,
)

logger = structlog.get_logger()


class InterviewAgent:
    """
    Checks candidate and interviewer availability, proposes interview
    slots, and creates interview drafts (pending approval).

    Owned by: Student 3 (Interview & Hiring Management)

    Allowed tools:
    - get_candidate_availability
    - get_interviewer_availability
    - get_calendar_availability
    - create_interview_draft

    NOT allowed:
    - send_interview_immediately (requires approval first)
    """

    def __init__(self):
        self.name = "InterviewAgent"
        self.allowed_tools = [
            "get_candidate_availability",
            "get_interviewer_availability",
            "get_calendar_availability",
            "create_interview_draft",
        ]
        self.tool_calls_log: list[dict[str, Any]] = []

    async def execute(
        self,
        input_data: InterviewSchedulingInput,
        auth_token: Optional[str] = None,
    ) -> InterviewSchedulingOutput:
        """Propose interview scheduling options."""
        logger.info(
            "interview_scheduling_started",
            application_id=input_data.application_id,
            interviewer_count=len(input_data.interviewer_ids),
        )

        # Determine date range (default: next 7 business days)
        if input_data.preferred_date_range_start:
            range_start = input_data.preferred_date_range_start
        else:
            range_start = (datetime.utcnow() + timedelta(days=1)).isoformat()

        if input_data.preferred_date_range_end:
            range_end = input_data.preferred_date_range_end
        else:
            range_end = (datetime.utcnow() + timedelta(days=10)).isoformat()

        # Step 1: Check candidate availability
        candidate_availability = await self._call_tool(
            "get_candidate_availability",
            get_candidate_availability,
            input_data.candidate_profile_id,
            range_start,
            range_end,
            auth_token,
        )

        # Step 2: Check each interviewer's availability
        interviewer_availabilities = []
        for interviewer_id in input_data.interviewer_ids:
            avail = await self._call_tool(
                "get_interviewer_availability",
                get_interviewer_availability,
                interviewer_id,
                range_start,
                range_end,
                auth_token,
            )
            interviewer_availabilities.append(avail or {})

        # Step 3: Check Google Calendar (optional)
        all_user_ids = [input_data.candidate_profile_id] + input_data.interviewer_ids
        calendar_data = await self._call_tool(
            "get_calendar_availability",
            get_calendar_availability,
            all_user_ids,
            range_start,
            range_end,
            auth_token,
        )

        # Step 4: Compute available slots
        candidate_interviews = (
            candidate_availability.get("existing_interviews", [])
            if candidate_availability else []
        )
        interviewer_interviews = []
        for avail in interviewer_availabilities:
            interviewer_interviews.extend(avail.get("busy_slots", []))

        slots = suggest_interview_slots(
            existing_candidate_interviews=candidate_interviews,
            existing_interviewer_interviews=interviewer_interviews,
            range_start=range_start,
            range_end=range_end,
            duration_minutes=60,
            max_slots=3,
        )

        # Step 5: Create draft proposal
        proposed_slots = [
            ProposedSlot(
                start_time=s["start_time"],
                end_time=s["end_time"],
                available_interviewers=input_data.interviewer_ids,
                all_available=True,
            )
            for s in slots
        ]

        if proposed_slots:
            draft = await self._call_tool(
                "create_interview_draft",
                create_interview_draft,
                input_data.application_id,
                [s.model_dump() for s in proposed_slots],
                input_data.interviewer_ids,
                "AI-proposed interview slots based on availability analysis",
                auth_token,
            )

        # Build output
        output = InterviewSchedulingOutput(
            application_id=input_data.application_id,
            proposed_slots=proposed_slots,
            recommendation=(
                f"Found {len(proposed_slots)} available slot(s) for interview."
                if proposed_slots
                else "No available slots found in the requested date range."
            ),
            notes=(
                "Google Calendar integration not yet active. "
                "Slots based on internal schedule only."
                if calendar_data and not calendar_data.get("calendar_connected")
                else ""
            ),
        )

        logger.info(
            "interview_scheduling_completed",
            application_id=input_data.application_id,
            slots_proposed=len(proposed_slots),
        )

        return output

    async def _call_tool(self, tool_name: str, tool_func, *args) -> Any:
        """Call a tool and log the invocation."""
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
            return result
        except Exception as e:
            duration_ms = int((time.time() - start) * 1000)
            self.tool_calls_log.append({
                "tool_name": tool_name,
                "duration_ms": duration_ms,
                "success": False,
                "error": str(e),
            })
            logger.error("tool_call_failed", tool=tool_name, error=str(e))
            return None
