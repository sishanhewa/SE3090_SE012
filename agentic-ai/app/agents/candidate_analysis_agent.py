"""Candidate Analysis Agent - Evaluates candidates against job requirements."""
import structlog
import re
from typing import Any, Optional

from app.clients.gemini_client import GeminiClient
from app.tools.candidate_tools import (
    get_application,
    get_job_requirements,
    get_application_resume,
)
from app.schemas.agent_schemas import (
    CandidateAnalysisInput,
    CandidateAnalysisOutput,
    SkillMatchResult,
)
from app.security.tool_permissions import check_tool_permission

logger = structlog.get_logger()


class CandidateAnalysisAgent:
    """
    Examines candidate profiles, skills, experience, qualifications,
    and job requirements to produce structured evaluation data.

    Owned by: Student 2 (Candidate & Application Management)

    Allowed tools:
    - get_job_requirements
    - get_candidate_profile
    - get_candidate_skills
    - get_candidate_experience
    - get_application_documents

    NOT allowed:
    - hire_candidate
    - reject_candidate
    - create_employee
    - send_offer
    """

    def __init__(self, gemini_client: Optional[GeminiClient] = None):
        self.name = "CandidateAnalysisAgent"
        self.allowed_tools = [
            "get_application",
            "get_job_requirements",
            "get_application_resume",
        ]
        self.gemini_client = gemini_client or GeminiClient()
        self.tool_calls_log: list[dict[str, Any]] = []

    async def execute(
        self,
        input_data: CandidateAnalysisInput,
        auth_token: Optional[str] = None,
    ) -> CandidateAnalysisOutput:
        """Analyze a candidate against job requirements."""
        logger.info(
            "candidate_analysis_started",
            application_id=input_data.application_id,
            job_id=input_data.job_id,
        )

        # Step 1: Gather data using allowed tools
        app_data = await self._call_tool(
            "get_application",
            get_application, input_data.application_id, auth_token
        )
        if not app_data:
            raise ValueError("Application details could not be loaded")
        if not app_data.get("resume_document_id"):
            raise ValueError("No CV is attached to this application")

        resume = await self._call_tool(
            "get_application_resume", get_application_resume, input_data.application_id, auth_token
        )
        if not resume:
            raise ValueError("The attached CV could not be read")
        cv_text = resume["text"]

        job_data = await self._call_tool(
            "get_job_requirements",
            get_job_requirements, input_data.job_id, auth_token
        )
        if not job_data:
            raise ValueError("Job requirements could not be loaded")

        # Step 2: Match skills and requirements
        job_reqs = job_data.get("requirements", []) if job_data else []
        mandatory_matches = self._match_skills(
            job_reqs,
            cv_text,
            mandatory_only=True,
        )

        preferred_matches = self._match_skills(
            job_reqs,
            cv_text,
            mandatory_only=False,
        )

        # Step 3: Calculate experience
        year_mentions = [float(value) for value in re.findall(r"\b(\d{1,2}(?:\.\d)?)\s*\+?\s*(?:years?|yrs?)\b", cv_text, re.IGNORECASE)]
        total_experience = max(year_mentions, default=0.0)
        min_required = job_data.get("minimum_experience", 0) if job_data else 0

        # Step 4: Summarize education
        education_summary = "Education evidence in CV requires human review."

        # Step 5: Use Gemini for qualitative analysis
        qualitative_analysis = ""
        try:
            analysis_result = await self.gemini_client.analyze_candidate(
                job_requirements=job_data,
                candidate_data={
                    "cv_skill_evidence": [m.skill_name for m in mandatory_matches + preferred_matches if m.is_matched],
                    "cv_years_mentioned": total_experience,
                    "source": "Applicant CV (personal details withheld)",
                },
            )
            qualitative_analysis = analysis_result.get("analysis", "")
        except Exception as e:
            logger.error("gemini_analysis_failed", error=str(e))
            qualitative_analysis = f"LLM analysis unavailable: {str(e)}"

        # Step 6: Build output
        output = CandidateAnalysisOutput(
            application_id=input_data.application_id,
            job_id=input_data.job_id,
            candidate_name=app_data.get("candidate_name", "Unknown")[:100],
            mandatory_skill_matches=mandatory_matches,
            preferred_skill_matches=preferred_matches,
            total_experience_years=total_experience,
            minimum_required_years=min_required,
            meets_experience_requirement=total_experience >= min_required,
            education_summary=education_summary,
            qualitative_analysis=qualitative_analysis,
            evidence_summary=f"Source: applicant CV ({resume['file_name']}). " + self._build_evidence_summary(
                mandatory_matches, preferred_matches, total_experience, min_required
            ),
        )

        logger.info(
            "candidate_analysis_completed",
            application_id=input_data.application_id,
            mandatory_matched=sum(1 for m in mandatory_matches if m.is_matched),
            total_mandatory=len(mandatory_matches),
            experience_years=total_experience,
        )

        return output

    async def _call_tool(self, tool_name: str, tool_func, *args) -> Any:
        """Call a tool and log the invocation."""
        import time
        check_tool_permission(self.name, tool_name)
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

    def _match_skills(
        self,
        job_requirements: list[dict],
        cv_text: str,
        mandatory_only: bool,
    ) -> list[SkillMatchResult]:
        """Match candidate skills against job requirements."""
        matches = []
        normalized_cv = re.sub(r"[^a-z0-9+#.]+", " ", cv_text.lower())

        for req in job_requirements:
            is_mandatory = req.get("isMandatory", True)
            if mandatory_only and not is_mandatory:
                continue
            if not mandatory_only and is_mandatory:
                continue

            desc = req.get("description", req.get("skillName", ""))
            if desc.startswith("Skill: "):
                req_skill_name = desc.replace("Skill: ", "").strip().lower()
                display_name = req_skill_name
            elif desc.startswith("Minimum Education: "):
                req_skill_name = desc.lower()
                display_name = desc
            else:
                req_skill_name = desc.lower()
                display_name = desc

            if desc.lower().startswith("minimum education:"):
                level = req_skill_name.split(":", 1)[-1].strip()
                terms = ["bachelor", "bsc"] if "bachelor" in level else ["master", "msc"] if "master" in level else [level]
                is_matched = any(re.search(r"(?<!\w)" + re.escape(term) + r"(?!\w)", normalized_cv) for term in terms)
            else:
                is_matched = bool(re.search(r"(?<!\w)" + re.escape(req_skill_name) + r"(?!\w)", normalized_cv)) if req_skill_name else False

            matches.append(SkillMatchResult(
                skill_name=display_name,
                is_mandatory=is_mandatory,
                is_matched=is_matched,
                candidate_proficiency=None,
                candidate_years=0,
                weight=req.get("weight", 10),
            ))

        return matches

    def _calculate_experience(self, experiences: list[dict]) -> float:
        """Calculate total years of experience."""
        from datetime import datetime

        total_days = 0
        for exp in experiences:
            try:
                start = datetime.fromisoformat(
                    str(exp.get("startDate", "")).replace("Z", "+00:00")
                )
                end_str = exp.get("endDate")
                if end_str:
                    end = datetime.fromisoformat(str(end_str).replace("Z", "+00:00"))
                else:
                    end = datetime.now(start.tzinfo) if start.tzinfo else datetime.utcnow()
                total_days += max(0, (end - start).days)
            except (ValueError, TypeError):
                continue

        return round(total_days / 365.25, 1)

    def _summarize_education(self, education: list[dict]) -> str:
        """Create a brief education summary."""
        if not education:
            return "No education data available."

        summaries = []
        for edu in education:
            degree = edu.get("degree", "")
            field = edu.get("fieldOfStudy", "")
            institution = edu.get("institution", "")
            if degree:
                summaries.append(f"{degree} in {field} from {institution}".strip())

        return "; ".join(summaries) if summaries else "Education data available but incomplete."

    def _build_evidence_summary(
        self,
        mandatory: list[SkillMatchResult],
        preferred: list[SkillMatchResult],
        experience: float,
        min_required: int,
    ) -> str:
        """Build a factual evidence summary."""
        mandatory_matched = sum(1 for m in mandatory if m.is_matched)
        preferred_matched = sum(1 for m in preferred if m.is_matched)

        parts = [
            f"Mandatory skills: {mandatory_matched}/{len(mandatory)} matched.",
            f"Preferred skills: {preferred_matched}/{len(preferred)} matched.",
            f"Experience: {experience} years (minimum {min_required} required).",
        ]

        if experience < min_required:
            parts.append("⚠ Does not meet minimum experience requirement.")

        missing_mandatory = [m.skill_name for m in mandatory if not m.is_matched]
        if missing_mandatory:
            parts.append(f"Missing mandatory skills: {', '.join(missing_mandatory)}.")

        return " ".join(parts)
