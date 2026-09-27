"""Candidate Analysis Agent - Evaluates candidates against job requirements."""
import structlog
from typing import Any, Optional

from app.clients.gemini_client import GeminiClient
from app.tools.candidate_tools import (
    get_job_requirements,
    get_candidate_profile,
    get_candidate_skills,
    get_candidate_experience,
    get_candidate_education,
    get_application_documents,
)
from app.schemas.agent_schemas import (
    CandidateAnalysisInput,
    CandidateAnalysisOutput,
    SkillMatchResult,
)

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
            "get_job_requirements",
            "get_candidate_profile",
            "get_candidate_skills",
            "get_candidate_experience",
            "get_application_documents",
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
        job_data = await self._call_tool(
            "get_job_requirements",
            get_job_requirements, input_data.job_id, auth_token
        )

        profile_data = await self._call_tool(
            "get_candidate_profile",
            get_candidate_profile, input_data.application_id, auth_token
        )

        skills_data = await self._call_tool(
            "get_candidate_skills",
            get_candidate_skills, input_data.application_id, auth_token
        )

        experience_data = await self._call_tool(
            "get_candidate_experience",
            get_candidate_experience, input_data.application_id, auth_token
        )

        education_data = await self._call_tool(
            "get_candidate_education",
            get_candidate_education, input_data.application_id, auth_token
        )

        documents_data = await self._call_tool(
            "get_application_documents",
            get_application_documents, input_data.application_id, auth_token
        )

        # Step 2: Match skills
        mandatory_matches = self._match_skills(
            job_data.get("skill_requirements", []),
            skills_data or [],
            mandatory_only=True,
        )

        preferred_matches = self._match_skills(
            job_data.get("skill_requirements", []),
            skills_data or [],
            mandatory_only=False,
        )

        # Step 3: Calculate experience
        total_experience = self._calculate_experience(experience_data or [])
        min_required = job_data.get("minimum_experience", 0)

        # Step 4: Summarize education
        education_summary = self._summarize_education(education_data or [])

        # Step 5: Use Gemini for qualitative analysis
        qualitative_analysis = ""
        try:
            analysis_result = await self.gemini_client.analyze_candidate(
                job_requirements=job_data,
                candidate_data={
                    "profile": profile_data,
                    "skills": skills_data,
                    "experience": experience_data,
                    "education": education_data,
                    "documents": [
                        {"name": d.get("fileName", ""), "type": d.get("fileType", "")}
                        for d in (documents_data or [])
                    ],
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
            candidate_name=profile_data.get("summary", "Unknown")[:100] if profile_data else "Unknown",
            mandatory_skill_matches=mandatory_matches,
            preferred_skill_matches=preferred_matches,
            total_experience_years=total_experience,
            minimum_required_years=min_required,
            meets_experience_requirement=total_experience >= min_required,
            education_summary=education_summary,
            qualitative_analysis=qualitative_analysis,
            evidence_summary=self._build_evidence_summary(
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
        candidate_skills: list[dict],
        mandatory_only: bool,
    ) -> list[SkillMatchResult]:
        """Match candidate skills against job requirements."""
        matches = []
        candidate_skill_names = {
            s.get("skillName", "").lower(): s for s in candidate_skills
        }

        for req in job_requirements:
            is_mandatory = req.get("isMandatory", True)
            if mandatory_only and not is_mandatory:
                continue
            if not mandatory_only and is_mandatory:
                continue

            skill_name = req.get("skillName", req.get("name", "")).lower()
            candidate_skill = candidate_skill_names.get(skill_name)

            matches.append(SkillMatchResult(
                skill_name=req.get("skillName", req.get("name", "")),
                is_mandatory=is_mandatory,
                is_matched=candidate_skill is not None,
                candidate_proficiency=candidate_skill.get("proficiencyLevel") if candidate_skill else None,
                candidate_years=candidate_skill.get("yearsOfExperience", 0) if candidate_skill else 0,
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
