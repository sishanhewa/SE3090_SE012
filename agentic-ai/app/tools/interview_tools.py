"""Interview Scheduling Agent tools — availability checks and draft creation."""
import httpx
import structlog
from typing import Any, Optional
from datetime import datetime, timedelta, timezone

logger = structlog.get_logger()

import os

from app.clients.backend_url import backend_api_url

BACKEND_BASE_URL = backend_api_url()


async def get_candidate_availability(
    application_id: str,
    date_range_start: str,
    date_range_end: str,
    auth_token: str | None = None,
) -> dict[str, Any]:
    """
    Check candidate interview availability within a date range.
    Returns existing interviews that would cause conflicts.
    
    Allowed tool for: InterviewAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        response = await client.get(
            f"{BACKEND_BASE_URL}/interviews/application/{application_id}",
            headers=_auth_headers(auth_token),
        )
        response.raise_for_status()
        payload = response.json()
        interviews = payload.get("items", []) if isinstance(payload, dict) else payload
        active = [item for item in interviews if item.get("status") not in ("Cancelled", "NoShow")]
        return {
            "application_id": application_id,
            "date_range": {"start": date_range_start, "end": date_range_end},
            "existing_interviews": [{
                "scheduled_at": item.get("scheduledAt", ""),
                "duration_minutes": item.get("durationMinutes", 60),
            } for item in active],
            "has_conflicts": bool(active),
        }


async def get_interviewer_availability(
    interviewer_id: str,
    date_range_start: str,
    date_range_end: str,
    auth_token: Optional[str] = None,
) -> dict[str, Any]:
    """
    Check interviewer availability by looking at their existing panel memberships.
    
    Allowed tool for: InterviewAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)

        response = await client.get(
            f"{BACKEND_BASE_URL}/interviews",
            params={
                "interviewerId": interviewer_id,
                "fromDate": date_range_start,
                "toDate": date_range_end,
            },
            headers=headers,
        )
        response.raise_for_status()
        interviews = response.json()

        busy_slots = []
        for interview in interviews.get("items", interviews) if isinstance(interviews, dict) else interviews:
            busy_slots.append({
                "scheduled_at": interview.get("scheduledAt", ""),
                "duration_minutes": interview.get("durationMinutes", 60),
            })

        logger.info(
            "tool_get_interviewer_availability",
            interviewer_id=interviewer_id,
            busy_slots=len(busy_slots),
        )
        return {
            "interviewer_id": interviewer_id,
            "date_range": {"start": date_range_start, "end": date_range_end},
            "busy_slots": busy_slots,
        }


async def get_calendar_availability(
    user_ids: list[str],
    date_range_start: str,
    date_range_end: str,
    auth_token: Optional[str] = None,
) -> dict[str, Any]:
    """
    Check Google Calendar availability for multiple users.
    External availability is checked by the backend when it sends an invitation.
    
    Allowed tool for: InterviewAgent
    """
    logger.info(
        "tool_get_calendar_availability",
        user_count=len(user_ids),
        note="External availability is not verifiable during screening",
    )
    return {
        "user_ids": user_ids,
        "date_range": {"start": date_range_start, "end": date_range_end},
        "external_conflicts": None,
        "calendar_connected": False,
        "note": "Calendar availability will be checked before the invitation is sent.",
    }


async def create_interview_draft(
    application_id: str,
    proposed_slots: list[dict[str, Any]],
    panel_member_ids: list[str],
    notes: str = "",
    auth_token: Optional[str] = None,
) -> dict[str, Any]:
    """
    Create a draft interview proposal (NOT an actual interview).
    The interview is only created after human approval.
    
    Allowed tool for: InterviewAgent
    NOT allowed: Directly creating/sending interview invitations.
    """
    draft = {
        "application_id": application_id,
        "proposed_slots": proposed_slots,
        "panel_member_ids": panel_member_ids,
        "notes": notes,
        "status": "draft",
        "requires_approval": True,
    }

    logger.info(
        "tool_create_interview_draft",
        application_id=application_id,
        slot_count=len(proposed_slots),
        panel_size=len(panel_member_ids),
    )
    return draft


def suggest_interview_slots(
    existing_candidate_interviews: list[dict],
    existing_interviewer_interviews: list[dict],
    range_start: str,
    range_end: str,
    duration_minutes: int = 60,
    max_slots: int = 3,
) -> list[dict[str, str]]:
    """
    Locally compute available interview slots avoiding conflicts.
    Business hours only (9 AM - 5 PM), weekdays only.
    """
    def as_utc(value: str) -> datetime:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
        return parsed.replace(tzinfo=timezone.utc) if parsed.tzinfo is None else parsed.astimezone(timezone.utc)

    start = as_utc(range_start)
    end = as_utc(range_end)

    # Collect all busy periods
    busy_periods = []
    for interview in existing_candidate_interviews + existing_interviewer_interviews:
        scheduled = interview.get("scheduled_at", "")
        if scheduled:
            s = as_utc(scheduled)
            dur = interview.get("duration_minutes", 60)
            busy_periods.append((s, s + timedelta(minutes=dur)))

    slots = []
    current = start.replace(hour=9, minute=0, second=0, microsecond=0)
    if current < start:
        current += timedelta(days=1)

    while current + timedelta(minutes=duration_minutes) <= end and len(slots) < max_slots:
        # Skip weekends
        if current.weekday() >= 5:
            current += timedelta(days=1)
            current = current.replace(hour=9, minute=0)
            continue

        # Skip outside business hours
        if current.hour < 9 or current.hour >= 17:
            current += timedelta(days=1)
            current = current.replace(hour=9, minute=0)
            continue

        slot_end = current + timedelta(minutes=duration_minutes)

        # Check for conflicts
        has_conflict = any(
            current < busy_end and slot_end > busy_start
            for busy_start, busy_end in busy_periods
        )

        if not has_conflict:
            slots.append({
                "start_time": current.isoformat(),
                "end_time": slot_end.isoformat(),
            })

        current += timedelta(hours=1)

    return slots


def _auth_headers(token: Optional[str] = None) -> dict[str, str]:
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return headers
