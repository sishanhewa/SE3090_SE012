# ADR-001: React State Management

## Status
Accepted

## Context
The React web application requires state management for:
- Server state (API data: jobs, applications, interviews, workflows)
- Client state (UI state: sidebar, modals, filters)
- Auth state (JWT tokens, user role, permissions)

Options considered:
1. **Redux Toolkit** — Industry standard, verbose, powerful devtools
2. **Zustand + TanStack Query** — Lightweight, minimal boilerplate, separates server/client state
3. **Context API + useReducer** — Built-in, no dependencies, limited for complex state

## Decision
**Zustand** for client/auth state + **TanStack Query** for server state.

## Rationale
- TanStack Query handles caching, refetching, loading/error states for API data automatically
- Zustand provides simple, TypeScript-friendly client state with minimal boilerplate
- Clear separation of concerns: server state vs UI state
- Much less code than Redux Toolkit for equivalent functionality

## Consequences
- Team must learn TanStack Query's caching and invalidation patterns
- Zustand stores should remain small and focused
- No Redux DevTools (Zustand has its own devtools middleware)
