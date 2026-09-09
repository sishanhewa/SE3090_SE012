# ADR-002: Flutter State Management

## Status
Accepted

## Context
The Flutter mobile application needs state management for:
- API communication and caching
- Authentication and secure token storage
- Form state and validation
- Navigation state

Options considered:
1. **Provider** — Simple, Flutter-recommended, limited scalability
2. **Riverpod** — Type-safe, testable, compile-time safety, auto-dispose
3. **BLoC/Cubit** — Event-driven, good separation, verbose

## Decision
**Riverpod** for all state management in the Flutter application.

## Rationale
- Compile-time safety catches dependency issues early
- Auto-dispose prevents memory leaks
- Excellent testability with provider overrides
- Works well with Dio for API integration
- Less boilerplate than BLoC while being more powerful than Provider

## Consequences
- Team must understand Riverpod's provider types (Provider, StateProvider, FutureProvider, etc.)
- Code generation with riverpod_generator recommended for complex providers
- All team members need familiarity with Riverpod patterns
