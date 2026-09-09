# ADR-004: Workflow State Persistence

## Status
Accepted

## Context
The Agentic AI workflow state must be persisted durably, including workflow ID, objective, plan, completed steps, tool results, validation results, errors, approval status, and final outcome.

Options considered:
1. **PostgreSQL structured tables** — Relational, queryable, consistent with main DB
2. **JSON document in PostgreSQL** — Flexible but harder to query
3. **Separate MongoDB** — Document-native but adds infrastructure complexity
4. **Redis** — Fast but not durable for audit trails

## Decision
**PostgreSQL structured tables** for all workflow state persistence.

## Rationale
- Consistent with the existing PostgreSQL database
- Structured tables enable efficient querying for dashboards and audit trails
- EF Core migrations manage schema evolution
- Foreign key relationships maintain data integrity
- No additional infrastructure needed

## Tables
- `WorkflowExecutions` — Objective, status, plan, final result
- `AgentSteps` — Agent name, step order, input/output, timing
- `ToolCalls` — Tool name, validated input/output, duration
- `WorkflowValidationResults` — Validation type, pass/fail, errors/warnings
- `WorkflowApprovals` — Requested action, decision, decided by/at

## Consequences
- Schema must be designed upfront for all workflow data
- JSON columns used for flexible structured data (plan, tool I/O)
- Audit trail is queryable and can power React dashboard views
