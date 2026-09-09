"""Pydantic schemas for workflow state."""
from pydantic import BaseModel, Field
from typing import Optional
from enum import Enum


class WorkflowStatusEnum(str, Enum):
    PLANNING = "Planning"
    IN_PROGRESS = "InProgress"
    AWAITING_APPROVAL = "AwaitingApproval"
    APPROVED = "Approved"
    REJECTED = "Rejected"
    COMPLETED = "Completed"
    FAILED = "Failed"


class WorkflowStep(BaseModel):
    step: int
    agent: str
    task: str
    status: str = "Pending"


class WorkflowPlan(BaseModel):
    workflow_id: str
    objective: str
    steps: list[WorkflowStep]


class WorkflowRequest(BaseModel):
    """Request to start a recruitment screening workflow."""
    application_id: str = Field(..., description="The application ID to screen")
    job_id: str = Field(..., description="The job ID to screen against")
    initiated_by: str = Field(..., description="User ID who initiated the workflow")
    company_id: str = Field(..., description="Company ID for isolation")


class WorkflowResponse(BaseModel):
    """Response from a workflow execution."""
    workflow_id: str
    status: WorkflowStatusEnum
    plan: Optional[WorkflowPlan] = None
    result: Optional[dict] = None
    error: Optional[str] = None
