import httpx
from typing import Optional, List, Dict, Any
from langchain_core.tools import tool
import os
import structlog
from app.agents.candidate_analysis_agent import CandidateAnalysisAgent
from app.agents.interview_agent import InterviewAgent
from app.agents.validation_agent import ValidationAgent
from app.schemas.agent_schemas import CandidateAnalysisInput, InterviewSchedulingInput, ValidationInput
from app.clients.gemini_client import GeminiClient

logger = structlog.get_logger()
from app.clients.backend_url import backend_api_url

BACKEND_BASE_URL = backend_api_url()

def get_auth_headers(token: Optional[str]) -> dict:
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return headers

def build_chat_tools(auth_token: Optional[str], company_id: str) -> List[Any]:

    @tool
    async def get_jobs() -> List[Dict]:
        """Fetch all available jobs. Use this to find the job ID if not known."""
        async with httpx.AsyncClient(timeout=30.0) as client:
            try:
                response = await client.get(
                    f"{BACKEND_BASE_URL}/jobs",
                    headers=get_auth_headers(auth_token)
                )
                response.raise_for_status()
                data = response.json()
                return [{"id": j["id"], "title": j["title"]} for j in data.get("items", [])]
            except Exception as e:
                logger.error("tool_get_jobs_failed", error=str(e))
                return []

    @tool
    async def get_job_requirements(job_id: str) -> Dict:
        """Fetch job requirements including skills, experience, and qualifications."""
        async with httpx.AsyncClient(timeout=30.0) as client:
            try:
                response = await client.get(
                    f"{BACKEND_BASE_URL}/jobs/{job_id}",
                    headers=get_auth_headers(auth_token)
                )
                response.raise_for_status()
                return response.json()
            except Exception as e:
                logger.error("tool_get_job_requirements_failed", error=str(e))
                return {}

    @tool
    async def get_applications_for_job(job_id: str) -> List[Dict]:
        """Get all applications for a specific job ID. Returns a list of applications containing the applicationId and candidate profile details."""
        async with httpx.AsyncClient(timeout=30.0) as client:
            try:
                response = await client.get(
                    f"{BACKEND_BASE_URL}/applications?jobId={job_id}",
                    headers=get_auth_headers(auth_token)
                )
                response.raise_for_status()
                data = response.json()
                # Return essential data to keep context small
                return [{"id": a["id"], "candidateName": a.get("candidateProfile", {}).get("summary", "Unknown Candidate")} for a in data.get("items", [])]
            except Exception as e:
                logger.error("tool_get_applications_failed", error=str(e))
                return []

    @tool
    async def analyze_candidate_for_job(application_id: str, job_id: str) -> Dict:
        """Analyze a candidate's suitability for a job. This uses the CandidateAnalysisAgent internally."""
        try:
            agent = CandidateAnalysisAgent(GeminiClient())
            result = await agent.execute(CandidateAnalysisInput(
                application_id=application_id,
                job_id=job_id,
                workflow_id="chat-workflow"
            ), auth_token)
            return result.model_dump()
        except Exception as e:
            logger.error("tool_analyze_candidate_failed", error=str(e))
            return {"error": str(e)}

    @tool
    async def validate_candidate_result(application_id: str, job_id: str, candidate_analysis: Dict) -> Dict:
        """Validate a candidate's analysis results before making recommendations. Uses the ValidationAgent internally."""
        try:
            agent = ValidationAgent()
            result = await agent.execute(ValidationInput(
                workflow_id="chat-workflow",
                application_id=application_id,
                job_id=job_id,
                candidate_analysis=candidate_analysis
            ), auth_token)
            return result.model_dump()
        except Exception as e:
            logger.error("tool_validate_candidate_failed", error=str(e))
            return {"error": str(e)}

    @tool
    async def suggest_interview_times(application_id: str, job_id: str) -> Dict:
        """Suggest interview times for a candidate. Uses the InterviewAgent internally."""
        try:
            agent = InterviewAgent()
            result = await agent.execute(InterviewSchedulingInput(
                workflow_id="chat-workflow",
                application_id=application_id,
                candidate_profile_id=application_id,
                interviewer_ids=[]
            ), auth_token)
            return result.model_dump()
        except Exception as e:
            logger.error("tool_suggest_interview_failed", error=str(e))
            return {"error": str(e)}

    @tool
    async def create_job_post(title: str, description: str, minimum_experience: int = 1, vacancy_count: int = 1) -> Dict:
        """Create a new job post. Use this when the user asks to create or post a new job."""
        async with httpx.AsyncClient(timeout=30.0) as client:
            try:
                # 1. Fetch an existing job to copy the CompanyId and DepartmentId
                jobs_response = await client.get(
                    f"{BACKEND_BASE_URL}/jobs",
                    headers=get_auth_headers(auth_token)
                )
                jobs_response.raise_for_status()
                jobs_data = jobs_response.json().get("items", [])

                if not jobs_data:
                    return {"error": "Cannot create job: No existing jobs found to infer Company and Department."}

                ref_job = jobs_data[0]
                company_id = ref_job.get("companyId")
                department_id = ref_job.get("departmentId")

                # 2. Create the new job
                payload = {
                    "title": title,
                    "description": description,
                    "minimumExperience": minimum_experience,
                    "vacancyCount": vacancy_count,
                    "departmentId": department_id
                }

                create_response = await client.post(
                    f"{BACKEND_BASE_URL}/companies/{company_id}/jobs",
                    json=payload,
                    headers=get_auth_headers(auth_token)
                )
                create_response.raise_for_status()

                return {"success": True, "message": f"Job '{title}' created successfully!", "job": create_response.json()}
            except Exception as e:
                logger.error("tool_create_job_failed", error=str(e))
                return {"error": str(e)}

    return [
        get_jobs,
        get_job_requirements,
        get_applications_for_job,
        analyze_candidate_for_job,
        validate_candidate_result,
        suggest_interview_times,
        create_job_post
    ]
