# ADR-005: Cloud Deployment Platform

## Status
Proposed

## Context
The assignment requires deployment of all components with working URLs.

Options considered:
1. **Render** — Free tier, supports .NET/Python/static, PostgreSQL available
2. **Railway** — Free tier, easy deployment, Docker support
3. **Azure** — Student credits available, native .NET support, complex setup
4. **Vercel + Neon** — Best for React + PostgreSQL, limited backend support

## Decision
- **ASP.NET Core API** → Render (free tier)
- **PostgreSQL** → Neon (free tier managed PostgreSQL)
- **React** → Vercel (free tier, optimized for frontend)
- **AI Service** → Render (free tier)
- **Flutter** → Android APK submitted directly

## Rationale
- All services are available on free tiers
- Render supports Docker deployments for .NET and Python
- Vercel provides excellent React deployment with CI/CD
- Neon offers managed PostgreSQL with generous free tier
- No student credit or payment required

## Consequences
- Free tier has cold start latency (acceptable for demo)
- Must verify free tier limits before submission
- Keep local setup instructions as fallback
