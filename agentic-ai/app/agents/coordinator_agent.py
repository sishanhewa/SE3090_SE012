"""Coordinator Agent - Plans and delegates the recruitment screening workflow."""
import json
import structlog
from typing import Any, Optional

from app.clients.gemini_client import GeminiClient
from app.schemas.agent_schemas import (
    WorkflowPlanOutput,
    PlanStep,
    WorkflowResult,
    CandidateAnalysisInput,
    ValidationInput,
    InterviewSchedulingInput,
)
from app.agents.candidate_analysis_agent import CandidateAnalysisAgent
from app.agents.validation_agent import ValidationAgent
from app.agents.interview_agent import InterviewAgent

logger = structlog.get_logger()


class CoordinatorAgent:
    """
    Receives a domain objective, creates a structured multi-step plan
    using Gemini, and delegates steps to appropriate agents.

    Owned by: Student 1 (Company & Job Management)

    Allowed tools:
    - create_plan
    - delegate_step
    - compile_results
    """

    def __init__(self, gemini_client: Optional[GeminiClient] = None):
        self.name = "CoordinatorAgent"
        self.allowed_tools = ["create_plan", "delegate_step", "compile_results"]
        self.gemini_client = gemini_client or GeminiClient()
        self.tool_calls_log: list[dict[str, Any]] = []

        # Agent registry
        self._agents = {
            "CandidateAnalysisAgent": CandidateAnalysisAgent(self.gemini_client),
            "ValidationAgent": ValidationAgent(),
            "InterviewAgent": InterviewAgent(),
        }

    async def execute(
        self,
        objective: dict[str, Any],
        auth_token: Optional[str] = None,
    ) -> WorkflowResult:
        """Execute the coordinator agent's planning and delegation phase."""
        workflow_id = objective.get("workflow_id", "unknown")
        application_id = objective.get("application_id", "")
        job_id = objective.get("job_id", "")

        logger.info(
            "coordinator_started",
            workflow_id=workflow_id,
            application_id=application_id,
        )

        # Step 1: Create plan using Gemini
        plan = await self._create_plan(objective)
        logger.info(
            "coordinator_plan_created",
            workflow_id=workflow_id,
            step_count=len(plan.steps),
        )

        # Step 2: Execute each step by delegating to agents
        step_results: dict[str, Any] = {}

        for step in plan.steps:
            logger.info(
                "coordinator_delegating",
                workflow_id=workflow_id,
                step=step.step_number,
                agent=step.agent,
                task=step.task,
            )

            try:
                result = await self._delegate_step(
                    step, workflow_id, application_id, job_id,
                    step_results, auth_token,
                )
                step_results[step.agent] = result

                # Merge agent's tool logs
                agent_instance = self._agents.get(step.agent)
                if agent_instance and hasattr(agent_instance, 'tool_calls_log'):
                    self.tool_calls_log.extend(agent_instance.tool_calls_log)

                logger.info(
                    "coordinator_step_completed",
                    step=step.step_number,
                    agent=step.agent,
                )
            except Exception as e:
                logger.error(
                    "coordinator_step_failed",
                    step=step.step_number,
                    agent=step.agent,
                    error=str(e),
                )
                step_results[step.agent] = {"error": str(e)}

                # Merge agent's tool logs even on failure if available
                agent_instance = self._agents.get(step.agent)
                if agent_instance and hasattr(agent_instance, 'tool_calls_log'):
                    self.tool_calls_log.extend(agent_instance.tool_calls_log)

        # Step 3: Compile final results
        final_result = await self._compile_results(
            workflow_id, application_id, job_id, plan, step_results
        )

        logger.info(
            "coordinator_completed",
            workflow_id=workflow_id,
            recommendation=final_result.overall_recommendation,
        )

        return final_result

    async def _create_plan(self, objective: dict[str, Any]) -> WorkflowPlanOutput:
        """Use Gemini to create a structured workflow plan."""
        try:
            plan = await self.gemini_client.generate_structured(
                prompt=f"""Create a recruitment screening plan for this objective:

Objective: {objective.get('objective', 'Evaluate candidate application')}
Application ID: {objective.get('application_id', '')}
Job ID: {objective.get('job_id', '')}

Available agents:
1. CandidateAnalysisAgent - Analyzes candidate skills, experience, education
2. ValidationAgent - Performs business rule validation checks
3. InterviewAgent - Proposes interview scheduling slots

Create an ordered plan with the correct sequence of agents to execute.
The plan must include all three agents in logical order.""",
                output_schema=WorkflowPlanOutput,
                system_instruction=(
                    "You are a recruitment workflow planner. "
                    "Create structured plans for candidate evaluation workflows. "
                    "Always include CandidateAnalysisAgent first, then ValidationAgent, "
                    "then InterviewAgent."
                ),
            )
            self.tool_calls_log.append({
                "tool_name": "create_plan",
                "success": True,
            })
            return plan
        except Exception as e:
            logger.warning("gemini_plan_failed_using_default", error=str(e))
            # Fallback: use deterministic default plan
            self.tool_calls_log.append({
                "tool_name": "create_plan",
                "success": False,
                "error": str(e),
                "used_fallback": True,
            })
            return WorkflowPlanOutput(
                objective=objective.get("objective", "Evaluate candidate application"),
                steps=[
                    PlanStep(
                        step_number=1,
                        agent="CandidateAnalysisAgent",
                        task="Analyse candidate profile against job requirements",
                        required_tools=["get_job_requirements", "get_candidate_profile",
                                        "get_candidate_skills", "get_candidate_experience"],
                    ),
                    PlanStep(
                        step_number=2,
                        agent="ValidationAgent",
                        task="Validate analysis results and scoring",
                        required_tools=["validate_application_state", "validate_scoring",
                                        "validate_schema"],
                        depends_on=[1],
                    ),
                    PlanStep(
                        step_number=3,
                        agent="InterviewAgent",
                        task="Propose interview scheduling slots",
                        required_tools=["get_candidate_availability",
                                        "get_interviewer_availability",
                                        "create_interview_draft"],
                        depends_on=[2],
                    ),
                ],
                reasoning="Default sequential plan: analyze → validate → schedule",
            )

    async def _delegate_step(
        self,
        step: PlanStep,
        workflow_id: str,
        application_id: str,
        job_id: str,
        previous_results: dict[str, Any],
        auth_token: Optional[str] = None,
    ) -> Any:
        """Delegate a step to the appropriate agent."""
        agent = self._agents.get(step.agent)
        if not agent:
            raise ValueError(f"Unknown agent: {step.agent}")

        if step.agent == "CandidateAnalysisAgent":
            input_data = CandidateAnalysisInput(
                application_id=application_id,
                job_id=job_id,
                workflow_id=workflow_id,
            )
            result = await agent.execute(input_data, auth_token)
            return result

        elif step.agent == "ValidationAgent":
            candidate_result = previous_results.get("CandidateAnalysisAgent")
            input_data = ValidationInput(
                workflow_id=workflow_id,
                application_id=application_id,
                job_id=job_id,
                candidate_analysis=candidate_result if hasattr(candidate_result, 'model_dump') else None,
            )
            result = await agent.execute(input_data, auth_token)
            return result

        elif step.agent == "InterviewAgent":
            input_data = InterviewSchedulingInput(
                workflow_id=workflow_id,
                application_id=application_id,
                candidate_profile_id=application_id,  # Will be resolved by tool
                interviewer_ids=[],  # Will be populated from job data
            )
            result = await agent.execute(input_data, auth_token)
            return result

        else:
            raise ValueError(f"No delegation handler for agent: {step.agent}")

    async def _compile_results(
        self,
        workflow_id: str,
        application_id: str,
        job_id: str,
        plan: WorkflowPlanOutput,
        step_results: dict[str, Any],
    ) -> WorkflowResult:
        """Compile all agent results into a final workflow result."""
        candidate_analysis = step_results.get("CandidateAnalysisAgent")
        validation = step_results.get("ValidationAgent")
        interview = step_results.get("InterviewAgent")

        # Determine overall recommendation
        is_valid = True
        if validation and hasattr(validation, 'is_valid'):
            is_valid = validation.is_valid

        has_slots = False
        if interview and hasattr(interview, 'proposed_slots'):
            has_slots = len(interview.proposed_slots) > 0

        if is_valid and has_slots:
            recommendation = "Proceed to interview — awaiting manager approval"
        elif is_valid:
            recommendation = "Candidate eligible but no interview slots available"
        else:
            errors = validation.errors if validation and hasattr(validation, 'errors') else []
            recommendation = f"Validation failed: {'; '.join(errors)}"

        # Build summary
        summary_parts = []
        if candidate_analysis and hasattr(candidate_analysis, 'evidence_summary'):
            summary_parts.append(candidate_analysis.evidence_summary)
        if validation and hasattr(validation, 'risk_level'):
            summary_parts.append(f"Risk level: {validation.risk_level}")
        if interview and hasattr(interview, 'recommendation'):
            summary_parts.append(interview.recommendation)

        self.tool_calls_log.append({
            "tool_name": "compile_results",
            "success": True,
        })

        return WorkflowResult(
            workflow_id=workflow_id,
            application_id=application_id,
            job_id=job_id,
            candidate_analysis=candidate_analysis if hasattr(candidate_analysis, 'model_dump') else None,
            validation=validation if hasattr(validation, 'model_dump') else None,
            interview_proposal=interview if hasattr(interview, 'model_dump') else None,
            overall_recommendation=recommendation,
            requires_approval=True,
            summary=" | ".join(summary_parts),
        )
