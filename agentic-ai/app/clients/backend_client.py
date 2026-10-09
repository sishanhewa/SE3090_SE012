"""Client for calling back to the .NET backend to persist workflow results."""
from app.clients.backend_url import backend_api_url
import json
import structlog
import httpx
from typing import Any
from dotenv import load_dotenv

load_dotenv()

logger = structlog.get_logger()


BACKEND_URL = backend_api_url()


async def push_workflow_results(
    workflow_id: str,
    final_state: dict[str, Any],
    auth_token: str = "",
) -> bool:
    """
    Push the completed workflow results back to the .NET backend
    via POST /api/workflows/callback.
    """
    # Build agent steps from step_logs and sub-results
    agent_steps = []
    step_order = 1

    step_logs = final_state.get("step_logs", [])

    if final_state.get("plan"):
        agent_steps.append({
            "agentName": "CoordinatorAgent",
            "stepOrder": step_order,
            "status": "Completed",
            "input": json.dumps({"objective": final_state.get("plan", {}).get("objective")}),
            "output": json.dumps(final_state["plan"]),
            "toolCalls": [],
        })
        step_order += 1
    
    def extract_tools_for_agent(agent_prefix: str):
        tools = []
        for log in step_logs:
            # Simple heuristic: tools belong to the agent that ran them.
            # E.g. CandidateAnalysisAgent tools: get_job_requirements etc
            if log.get("success") is not None:
                # Let's map tools based on their names to agents
                tool_name = log.get("tool_name", "")
                is_candidate = tool_name in ["get_application", "get_job_requirements", "get_application_resume", "analyze_candidate"]
                is_validation = tool_name in ["validate_application_state", "validate_scoring", "validate_schema"]
                is_interview = tool_name in ["get_candidate_availability", "get_interviewer_availability", "create_interview_draft"]
                
                if (agent_prefix == "CandidateAnalysisAgent" and is_candidate) or \
                   (agent_prefix == "ValidationAgent" and is_validation) or \
                   (agent_prefix == "InterviewAgent" and is_interview) or \
                   (agent_prefix == "CoordinatorAgent" and tool_name in ["create_plan", "compile_results"]):
                    
                    tools.append({
                        "toolName": tool_name,
                        "validated": log.get("success", False),
                        "durationMs": log.get("duration_ms", 0),
                        "input": json.dumps({"error": log.get("error")}) if log.get("error") else None,
                        "output": None
                    })
        return tools

    # Extract candidate analysis step
    ca = final_state.get("candidate_analysis")
    if ca is not None or "CandidateAnalysisAgent" in [t.get("agent", "") for t in final_state.get("plan", {}).get("steps", [])] or any(log.get("tool_name") == "get_job_requirements" for log in step_logs):
        agent_steps.append({
            "agentName": "CandidateAnalysisAgent",
            "stepOrder": step_order,
            "status": "Completed" if ca and not isinstance(ca, dict) or (isinstance(ca, dict) and "error" not in ca) else "Failed",
            "input": json.dumps({"application_id": final_state.get("application_id"), "job_id": final_state.get("job_id")}),
            "output": json.dumps(ca) if isinstance(ca, dict) else str(ca) if ca else json.dumps({"error": "Failed to generate analysis"}),
            "toolCalls": extract_tools_for_agent("CandidateAnalysisAgent"),
        })
        step_order += 1

    # Extract validation step
    vr = final_state.get("validation_result")
    if vr is not None or any(log.get("tool_name") == "validate_scoring" for log in step_logs):
        agent_steps.append({
            "agentName": "ValidationAgent",
            "stepOrder": step_order,
            "status": "Completed" if vr and not isinstance(vr, dict) or (isinstance(vr, dict) and "error" not in vr) else "Failed",
            "input": json.dumps({"workflow_id": workflow_id}),
            "output": json.dumps(vr) if isinstance(vr, dict) else str(vr) if vr else json.dumps({"error": "Failed to validate"}),
            "toolCalls": extract_tools_for_agent("ValidationAgent"),
        })
        step_order += 1

    # Extract interview step
    ip = final_state.get("interview_proposal")
    if ip is not None or any(log.get("tool_name") == "get_candidate_availability" for log in step_logs):
        agent_steps.append({
            "agentName": "InterviewAgent",
            "stepOrder": step_order,
            "status": "Completed" if ip and not isinstance(ip, dict) or (isinstance(ip, dict) and "error" not in ip) else "Failed",
            "input": json.dumps({"application_id": final_state.get("application_id")}),
            "output": json.dumps(ip) if isinstance(ip, dict) else str(ip) if ip else json.dumps({"error": "Failed to generate interview slots"}),
            "toolCalls": extract_tools_for_agent("InterviewAgent"),
        })
        step_order += 1



    # Build the final result summary
    final_result_data = final_state.get("final_result")
    final_result_str = None
    if final_result_data:
        if isinstance(final_result_data, dict):
            final_result_str = json.dumps(final_result_data)
        else:
            final_result_str = str(final_result_data)

    # Map status
    status = final_state.get("status", "Failed")
    # Map Python enum values to C# enum names
    status_map = {
        "Planning": "Planning",
        "InProgress": "InProgress",
        "AwaitingApproval": "AwaitingApproval",
        "Approved": "Approved",
        "Rejected": "Rejected",
        "Failed": "Failed",
        "Completed": "Completed",
    }
    mapped_status = status_map.get(status, status)

    payload = {
        "workflowId": workflow_id,
        "status": mapped_status,
        "plan": json.dumps(final_state["plan"]) if final_state.get("plan") else None,
        "finalResult": final_result_str,
        "errorDetails": final_state.get("error"),
        "agentSteps": agent_steps,
    }

    try:
        async with httpx.AsyncClient(timeout=30.0) as client:
            url = f"{BACKEND_URL}/workflows/callback"
            logger.info(
                "pushing_results_to_backend",
                url=url,
                workflow_id=workflow_id,
                status=mapped_status,
                agent_steps_count=len(agent_steps),
            )
            headers = {"Authorization": f"Bearer {auth_token}"} if auth_token else {}
            response = await client.post(url, json=payload, headers=headers)

            if response.status_code == 200:
                logger.info(
                    "backend_callback_success",
                    workflow_id=workflow_id,
                )
                return True
            else:
                logger.error(
                    "backend_callback_failed",
                    workflow_id=workflow_id,
                    status_code=response.status_code,
                    response=response.text[:500],
                )
                return False

    except Exception as e:
        logger.error(
            "backend_callback_error",
            workflow_id=workflow_id,
            error=str(e),
        )
        return False


def _extract_tool_calls(step_logs: list[dict], agent_name: str) -> list[dict]:
    """Extract tool calls from step logs for a specific agent."""
    tool_calls = []
    for log_entry in step_logs:
        tool_name = log_entry.get("tool_name", "")
        success = log_entry.get("success", True)
        tool_calls.append({
            "toolName": tool_name,
            "input": json.dumps(log_entry.get("input")) if log_entry.get("input") else None,
            "output": json.dumps(log_entry.get("output")) if log_entry.get("output") else None,
            "validated": success,
            "durationMs": log_entry.get("duration_ms", 50),
        })
    return tool_calls
