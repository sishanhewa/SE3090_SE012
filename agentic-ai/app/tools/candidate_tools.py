"""Candidate Analysis Agent tools — read-only data retrieval from backend API."""
import httpx
import structlog
from typing import Any, Optional

logger = structlog.get_logger()

import os

# Base URL for the ASP.NET backend API
BACKEND_BASE_URL = os.getenv("BACKEND_URL", "http://localhost:5155/api")


async def get_application(
    application_id: str, auth_token: Optional[str] = None
) -> dict[str, Any]:
    """Fetch application details including the candidate_profile_id."""
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)
        response = await client.get(
            f"{BACKEND_BASE_URL}/applications/{application_id}", headers=headers
        )
        response.raise_for_status()
        app_data = response.json()
        
        logger.info("tool_get_application", application_id=application_id, status="success")
        return {
            "application_id": application_id,
            "job_id": app_data.get("jobId", ""),
            "candidate_profile_id": app_data.get("candidateProfileId", ""),
            "status": app_data.get("status", 0),
            "cover_letter": app_data.get("coverLetter", ""),
        }

async def get_job_requirements(
    job_id: str, auth_token: Optional[str] = None
) -> dict[str, Any]:
    """
    Fetch job requirements including skills, experience, and qualifications.
    
    Allowed tool for: CandidateAnalysisAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)
        response = await client.get(
            f"{BACKEND_BASE_URL}/jobs/{job_id}", headers=headers
        )
        response.raise_for_status()
        job_data = response.json()

        logger.info("tool_get_job_requirements", job_id=job_id, status="success")
        return {
            "job_id": job_id,
            "title": job_data.get("title", ""),
            "description": job_data.get("description", ""),
            "minimum_experience": job_data.get("minimumExperience", 0),
            "vacancy_count": job_data.get("vacancyCount", 0),
            "employment_type": job_data.get("employmentType", ""),
            "skill_requirements": job_data.get("skillRequirements", []),
            "requirements": job_data.get("requirements", []),
        }


async def get_candidate_profile(
    candidate_profile_id: str, auth_token: Optional[str] = None
) -> dict[str, Any]:
    """
    Fetch candidate profile with summary and contact info.
    
    Allowed tool for: CandidateAnalysisAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)
        response = await client.get(
            f"{BACKEND_BASE_URL}/candidateprofiles/{candidate_profile_id}",
            headers=headers,
        )
        response.raise_for_status()
        profile = response.json()

        logger.info(
            "tool_get_candidate_profile",
            candidate_id=candidate_profile_id,
            status="success",
        )
        return {
            "candidate_profile_id": candidate_profile_id,
            "summary": profile.get("summary", ""),
            "phone": profile.get("phone", ""),
            "linkedin_url": profile.get("linkedInUrl", ""),
            "portfolio_url": profile.get("portfolioUrl", ""),
        }


async def get_candidate_skills(
    candidate_profile_id: str, auth_token: Optional[str] = None
) -> list[dict[str, Any]]:
    """
    Fetch candidate skills with proficiency levels and experience.
    
    Allowed tool for: CandidateAnalysisAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)
        response = await client.get(
            f"{BACKEND_BASE_URL}/candidateprofiles/{candidate_profile_id}",
            headers=headers,
        )
        response.raise_for_status()
        profile = response.json()

        skills = profile.get("skills", [])
        logger.info(
            "tool_get_candidate_skills",
            candidate_id=candidate_profile_id,
            skill_count=len(skills),
        )
        return skills


async def get_candidate_experience(
    candidate_profile_id: str, auth_token: Optional[str] = None
) -> list[dict[str, Any]]:
    """
    Fetch candidate work experience history.
    
    Allowed tool for: CandidateAnalysisAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)
        response = await client.get(
            f"{BACKEND_BASE_URL}/candidateprofiles/{candidate_profile_id}",
            headers=headers,
        )
        response.raise_for_status()
        profile = response.json()

        experience = profile.get("experience", [])
        logger.info(
            "tool_get_candidate_experience",
            candidate_id=candidate_profile_id,
            experience_count=len(experience),
        )
        return experience


async def get_candidate_education(
    candidate_profile_id: str, auth_token: Optional[str] = None
) -> list[dict[str, Any]]:
    """
    Fetch candidate education history.
    
    Allowed tool for: CandidateAnalysisAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)
        response = await client.get(
            f"{BACKEND_BASE_URL}/candidateprofiles/{candidate_profile_id}",
            headers=headers,
        )
        response.raise_for_status()
        profile = response.json()

        education = profile.get("education", [])
        logger.info(
            "tool_get_candidate_education",
            candidate_id=candidate_profile_id,
            education_count=len(education),
        )
        return education


async def get_application_documents(
    candidate_profile_id: str, auth_token: Optional[str] = None
) -> list[dict[str, Any]]:
    """
    Fetch candidate uploaded documents (CV, certificates, etc.).
    
    Allowed tool for: CandidateAnalysisAgent
    """
    async with httpx.AsyncClient(timeout=30.0) as client:
        headers = _auth_headers(auth_token)
        response = await client.get(
            f"{BACKEND_BASE_URL}/candidateprofiles/{candidate_profile_id}",
            headers=headers,
        )
        response.raise_for_status()
        profile = response.json()

        documents = profile.get("documents", [])
        logger.info(
            "tool_get_application_documents",
            candidate_id=candidate_profile_id,
            document_count=len(documents),
        )
        return documents


def _auth_headers(token: Optional[str] = None) -> dict[str, str]:
    """Build authorization headers."""
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    return headers
