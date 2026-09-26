"""TalentFlow AI - Agentic AI Service"""
from fastapi import FastAPI, HTTPException, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
import structlog

from app.schemas.workflow import WorkflowRequest, WorkflowResponse, WorkflowStatusEnum
from app.graph.workflow_graph import run_screening_workflow

logger = structlog.get_logger()

app = FastAPI(
    title="TalentFlow AI - Agentic Service",
    description="AI-powered recruitment screening and candidate assessment service",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Restrict in production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# In-memory workflow state cache (production would use Redis or DB)
_workflow_cache: dict[str, dict] = {}


@app.get("/health")
async def health_check():
    return {"status": "healthy", "service": "agentic-ai"}


@app.get("/")
async def root():
    return {"message": "TalentFlow AI - Agentic Service", "version": "1.0.0"}


@app.post("/api/screening/start", response_model=WorkflowResponse)
async def start_screening(
    request: WorkflowRequest,
    background_tasks: BackgroundTasks,
):
    """
    Start a recruitment screening workflow.
    The workflow runs asynchronously and can be polled for status.
    """
    import uuid
    workflow_id = str(uuid.uuid4())

    logger.info(
        "screening_workflow_requested",
        workflow_id=workflow_id,
        application_id=request.application_id,
        job_id=request.job_id,
    )

    # Initialize cache entry
    _workflow_cache[workflow_id] = {
        "status": WorkflowStatusEnum.PLANNING.value,
        "result": None,
        "error": None,
    }

    # Run workflow in background
    background_tasks.add_task(
        _execute_workflow,
        workflow_id,
        request,
    )

    return WorkflowResponse(
        workflow_id=workflow_id,
        status=WorkflowStatusEnum.PLANNING,
    )


@app.get("/api/screening/{workflow_id}", response_model=WorkflowResponse)
async def get_screening_status(workflow_id: str):
    """Get the current status and results of a screening workflow."""
    cached = _workflow_cache.get(workflow_id)
    if not cached:
        raise HTTPException(status_code=404, detail="Workflow not found")

    return WorkflowResponse(
        workflow_id=workflow_id,
        status=WorkflowStatusEnum(cached["status"]),
        result=cached.get("result"),
        error=cached.get("error"),
    )


@app.get("/api/agents")
async def list_agents():
    """List available agents and their allowed tools."""
    return {
        "agents": [
            {
                "name": "CoordinatorAgent",
                "owner": "Student 1",
                "tools": ["create_plan", "delegate_step", "compile_results"],
            },
            {
                "name": "CandidateAnalysisAgent",
                "owner": "Student 2",
                "tools": [
                    "get_job_requirements", "get_candidate_profile",
                    "get_candidate_skills", "get_candidate_experience",
                    "get_application_documents",
                ],
            },
            {
                "name": "InterviewAgent",
                "owner": "Student 3",
                "tools": [
                    "get_candidate_availability", "get_interviewer_availability",
                    "get_calendar_availability", "create_interview_draft",
                ],
            },
            {
                "name": "ValidationAgent",
                "owner": "Student 4",
                "tools": [
                    "validate_application_state", "validate_scoring",
                    "validate_scheduling", "validate_authorization",
                    "validate_schema",
                ],
            },
        ]
    }


async def _execute_workflow(workflow_id: str, request: WorkflowRequest):
    """Background task to execute the screening workflow."""
    try:
        result = await run_screening_workflow(
            workflow_id=workflow_id,
            application_id=request.application_id,
            job_id=request.job_id,
            company_id=request.company_id,
            initiated_by=request.initiated_by,
        )

        _workflow_cache[workflow_id] = {
            "status": result.get("status", WorkflowStatusEnum.FAILED.value),
            "result": result.get("final_result"),
            "error": result.get("error"),
        }

        logger.info(
            "screening_workflow_completed",
            workflow_id=workflow_id,
            status=result.get("status"),
        )

    except Exception as e:
        logger.error(
            "screening_workflow_failed",
            workflow_id=workflow_id,
            error=str(e),
        )
        _workflow_cache[workflow_id] = {
            "status": WorkflowStatusEnum.FAILED.value,
            "result": None,
            "error": str(e),
        }
