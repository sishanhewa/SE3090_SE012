# ADR-003: Agentic AI Framework and Orchestration

## Status
Accepted

## Context
The assignment requires a meaningful Agentic AI workflow with:
- Multi-step planning and delegation
- Distinct specialized agents
- Controlled tools with allow-lists
- Persisted workflow state
- Deterministic validation
- Human approval for high-impact actions

Options considered:
1. **LangGraph + Gemini** — Graph-based orchestration, used in labs, structured state
2. **Microsoft Agent Framework** — .NET native, newer, less mature
3. **LlamaIndex Agents** — Document-focused, less suited for multi-agent workflows
4. **Custom orchestration** — Full control, more development effort

## Decision
**LangGraph** for agent orchestration + **Google Gemini** as the LLM + **Pydantic** for validation.

## Rationale
- LangGraph provides graph-based state management ideal for multi-step workflows
- Used in labs, so team has some familiarity
- Gemini API has free tier suitable for development
- Pydantic enforces structured inputs/outputs for deterministic validation
- FastAPI integrates naturally with Pydantic and async Python

## Consequences
- AI service is Python-based, communicating with ASP.NET Core via internal HTTP
- Clients never call the AI service directly (mandatory backend rule)
- LLM outputs must be validated with Pydantic before use
- Deterministic scoring must be calculated by application code, not the LLM
