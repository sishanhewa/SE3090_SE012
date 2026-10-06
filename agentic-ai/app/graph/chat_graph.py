import os
import structlog
from typing import Optional, List, Any
from langchain_google_genai import ChatGoogleGenerativeAI
from langgraph.prebuilt import create_react_agent
from langchain_core.messages import HumanMessage, AIMessage, SystemMessage

from app.tools.chat_tools import build_chat_tools
from app.schemas.chat import ChatMessage

logger = structlog.get_logger()

async def run_chat_workflow(messages: List[ChatMessage], company_id: str, auth_token: Optional[str] = None) -> str:
    """Execute the chat workflow using LangGraph ReAct agent."""
    logger.info("chat_workflow_started", message_count=len(messages))

    api_keys = [v for k, v in os.environ.items() if k.startswith("GOOGLE_API_KEY") and v]
    if not api_keys: api_keys.append("")

    # Get tools with context injected
    tools = build_chat_tools(auth_token, company_id)

    # System prompt based on user requirements
    system_prompt = """You are an AI Recruitment Assistant (Coordinator Agent).
Your job is to answer the recruiter's questions by deciding what needs to happen, delegating tasks to other agents, and returning a clear answer.
You have access to tools that can:
1. Find jobs (get_jobs, get_job_requirements)
2. Find applications for a job (get_applications_for_job)
3. Analyze candidates using the CandidateAnalysisAgent (analyze_candidate_for_job)
4. Validate analysis using the ValidationAgent (validate_candidate_result)
5. Suggest interview times using the InterviewAgent (suggest_interview_times)
6. Create new job posts (create_job_post)

Workflow Rules:
- If a user asks to show suitable candidates for a job, you MUST:
  1. Find the job ID.
  2. Get all applications for the job.
  3. Analyze each candidate against the job requirements.
  4. Validate the results.
  5. Rank them based on their scores/qualitative analysis and present the top candidates.
- If a user asks a simple question like "How many candidates applied for X?", just get the applications and return the count.
- If a user asks to suggest interview times, use the suggest_interview_times tool.
- If a user asks to create or post a new job, use the create_job_post tool and ask them for any missing details like title or description.
- DO NOT automatically reject, hire, send offers, or send interview invitations. If the user asks for these, state that human approval is required.
- Present answers in a clear, friendly, and simple format for HR staff. Do NOT expose JSON or technical details. Explain the candidate's score in simple language.
"""

    # Agent graph initialization is moved into the rotation loop

    # Convert input messages to LangChain format
    lc_messages = []
    for m in messages:
        if m.role == "user":
            lc_messages.append(HumanMessage(content=m.content))
        elif m.role == "assistant":
            lc_messages.append(AIMessage(content=m.content))
        elif m.role == "system":
            lc_messages.append(SystemMessage(content=m.content))

    # Run the graph with API key rotation
    last_error = None
    for idx, api_key in enumerate(api_keys):
        try:
            llm = ChatGoogleGenerativeAI(
                model="gemini-3.8-flash",
                temperature=0.2,
                google_api_key=api_key,
            )
            agent_executor = create_react_agent(llm, tools, prompt=system_prompt)

            response_state = await agent_executor.ainvoke(
                {"messages": lc_messages}
            )

            # The final message is the assistant's response
            final_message = response_state["messages"][-1]
            content = final_message.content
            if isinstance(content, list):
                text_parts = [part.get("text", "") for part in content if isinstance(part, dict) and "text" in part]
                return "".join(text_parts)
            return str(content)
        except Exception as e:
            last_error = str(e)
            error_lower = last_error.lower()
            is_rate_limit = any(kw in error_lower for kw in ["429", "rate", "quota", "exhausted", "limit"])

            if is_rate_limit and idx < len(api_keys) - 1:
                logger.warning("chat_workflow_rate_limited", key_index=idx, error=last_error)
                continue # Try the next key
            else:
                logger.error("chat_workflow_failed", error=last_error)
                return f"An error occurred while processing your request: {last_error}"

    return f"An error occurred after trying all API keys: {last_error}"
