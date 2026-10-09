"""LangGraph workflow graph for the recruitment screening pipeline."""
import structlog
from typing import Any, TypedDict
from langgraph.graph import StateGraph, END

from app.agents.coordinator_agent import CoordinatorAgent
from app.schemas.workflow import WorkflowStatusEnum

logger = structlog.get_logger()


class WorkflowState(TypedDict):
    """Shared state across the workflow graph nodes."""
    workflow_id: str
    application_id: str
    job_id: str
    company_id: str
    initiated_by: str
    auth_token: str
    status: str
    plan: dict | None
    candidate_analysis: dict | None
    score_result: dict | None
    validation_result: dict | None
    interview_proposal: dict | None
    final_result: dict | None
    error: str | None
    step_logs: list[dict]


async def planning_node(state: WorkflowState) -> WorkflowState:
    """Node 1: Coordinator creates a structured plan."""
    logger.info("graph_planning_node", workflow_id=state["workflow_id"])

    coordinator = CoordinatorAgent()
    objective = {
        "workflow_id": state["workflow_id"],
        "application_id": state["application_id"],
        "job_id": state["job_id"],
        "objective": (
            f"Evaluate application {state['application_id']} "
            f"for job {state['job_id']} and determine whether "
            "the candidate should proceed to interview."
        ),
    }

    try:
        result = await coordinator.execute(objective, state.get("auth_token"))

        state["status"] = WorkflowStatusEnum.AWAITING_APPROVAL.value
        state["final_result"] = result.model_dump() if hasattr(result, 'model_dump') else {}
        if result.plan:
            state["plan"] = result.plan.model_dump()

        # Extract sub-results
        if result.candidate_analysis:
            state["candidate_analysis"] = result.candidate_analysis.model_dump()
        if result.validation:
            state["validation_result"] = result.validation.model_dump()
        if result.interview_proposal:
            state["interview_proposal"] = result.interview_proposal.model_dump()

        state["step_logs"].extend(coordinator.tool_calls_log)

    except Exception as e:
        logger.error("graph_planning_failed", error=str(e))
        state["status"] = WorkflowStatusEnum.FAILED.value
        state["error"] = str(e)

    return state


async def approval_check_node(state: WorkflowState) -> WorkflowState:
    """
    Node 2: Check if workflow needs approval (it always does for high-impact actions).
    This node transitions the workflow to AwaitingApproval state.
    The actual approval happens via the REST API, not in the graph.
    """
    logger.info(
        "graph_approval_check",
        workflow_id=state["workflow_id"],
        status=state["status"],
    )

    if state["status"] == WorkflowStatusEnum.FAILED.value:
        return state

    # Set status to awaiting approval — the graph pauses here
    state["status"] = WorkflowStatusEnum.AWAITING_APPROVAL.value
    return state


def should_continue(state: WorkflowState) -> str:
    """Conditional edge: determine next node based on state."""
    if state.get("error") or state["status"] == WorkflowStatusEnum.FAILED.value:
        return "end"
    return "approval_check"


def build_workflow_graph() -> StateGraph:
    """Build and compile the LangGraph workflow."""
    graph = StateGraph(WorkflowState)

    # Add nodes
    graph.add_node("planning", planning_node)
    graph.add_node("approval_check", approval_check_node)

    # Set entry point
    graph.set_entry_point("planning")

    # Add conditional edges
    graph.add_conditional_edges(
        "planning",
        should_continue,
        {
            "approval_check": "approval_check",
            "end": END,
        },
    )

    # Approval check always ends (waits for external approval via API)
    graph.add_edge("approval_check", END)

    return graph.compile()


async def run_screening_workflow(
    workflow_id: str,
    application_id: str,
    job_id: str,
    company_id: str,
    initiated_by: str,
    auth_token: str = "",
) -> dict[str, Any]:
    """
    Execute the full screening workflow graph.
    Returns the final state including results and status.
    """
    logger.info(
        "workflow_graph_starting",
        workflow_id=workflow_id,
        application_id=application_id,
    )

    initial_state: WorkflowState = {
        "workflow_id": workflow_id,
        "application_id": application_id,
        "job_id": job_id,
        "company_id": company_id,
        "initiated_by": initiated_by,
        "auth_token": auth_token,
        "status": WorkflowStatusEnum.PLANNING.value,
        "plan": None,
        "candidate_analysis": None,
        "score_result": None,
        "validation_result": None,
        "interview_proposal": None,
        "final_result": None,
        "error": None,
        "step_logs": [],
    }

    graph = build_workflow_graph()
    final_state = await graph.ainvoke(initial_state)

    logger.info(
        "workflow_graph_completed",
        workflow_id=workflow_id,
        final_status=final_state["status"],
    )

    return final_state
