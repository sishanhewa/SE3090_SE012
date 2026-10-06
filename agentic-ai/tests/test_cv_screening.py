"""The screening evidence must come from the CV attached to the application."""
from io import BytesIO

import httpx
import pytest
from docx import Document

from app.agents.candidate_analysis_agent import CandidateAnalysisAgent
from app.agents.coordinator_agent import CoordinatorAgent
from app.schemas.agent_schemas import (
    CandidateAnalysisOutput, ValidationOutput, WorkflowPlanOutput,
)
from app.tools import candidate_tools


def _docx_bytes(text: str) -> bytes:
    document = Document()
    document.add_paragraph(text)
    output = BytesIO()
    document.save(output)
    return output.getvalue()


@pytest.mark.asyncio
async def test_attached_docx_is_read_and_used_for_skill_matching(monkeypatch):
    class FakeClient:
        async def __aenter__(self):
            return self

        async def __aexit__(self, *_):
            return None

        async def get(self, url, headers):
            assert url.endswith("/applications/app-1/resume")
            assert headers["Authorization"] == "Bearer recruiter-token"
            return httpx.Response(
                200,
                content=_docx_bytes("Five years in software. 5 years of C# and .NET Core."),
                headers={"content-disposition": 'attachment; filename="candidate.docx"'},
                request=httpx.Request("GET", url),
            )

    monkeypatch.setattr(candidate_tools.httpx, "AsyncClient", lambda **_: FakeClient())
    resume = await candidate_tools.get_application_resume("app-1", "recruiter-token")
    agent = CandidateAnalysisAgent(gemini_client=object())
    matches = agent._match_skills(
        [{"description": "Skill: C#", "isMandatory": True},
         {"description": "Skill: Java", "isMandatory": True}],
        resume["text"],
        mandatory_only=True,
    )
    assert [match.is_matched for match in matches] == [True, False]


@pytest.mark.asyncio
async def test_unreadable_docx_fails_instead_of_screening_empty_evidence(monkeypatch):
    class FakeClient:
        async def __aenter__(self):
            return self

        async def __aexit__(self, *_):
            return None

        async def get(self, url, headers):
            return httpx.Response(
                200,
                content=_docx_bytes(""),
                headers={"content-disposition": 'attachment; filename="empty.docx"'},
                request=httpx.Request("GET", url),
            )

    monkeypatch.setattr(candidate_tools.httpx, "AsyncClient", lambda **_: FakeClient())
    with pytest.raises(ValueError, match="No readable text"):
        await candidate_tools.get_application_resume("app-1", "recruiter-token")


@pytest.mark.asyncio
@pytest.mark.parametrize("valid,expected", [(True, "Shortlist"), (False, "DoNotShortlist")])
async def test_screening_recommendation_comes_from_cv_checks_not_calendar_slots(valid, expected):
    coordinator = CoordinatorAgent(gemini_client=object())
    result = await coordinator._compile_results(
        "workflow-1", "app-1", "job-1",
        WorkflowPlanOutput(objective="Screen CV", steps=[], reasoning="Test"),
        {
            "CandidateAnalysisAgent": CandidateAnalysisOutput(
                application_id="app-1", job_id="job-1", evidence_summary="CV evidence"
            ),
            "ValidationAgent": ValidationOutput(is_valid=valid,
                errors=[] if valid else ["Missing mandatory skill"]),
        },
    )
    assert result.screening_recommendation == expected
    assert result.requires_approval is True
