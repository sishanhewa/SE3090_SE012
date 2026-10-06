# TalentFlow AI — Intelligent Recruitment & Workforce Management Platform

> SE3090 — Software Engineering Frameworks | Assignment 1 | Group SE012

## 📋 Overview

TalentFlow AI is a full-stack, AI-powered recruitment and workforce management platform that manages the complete hiring lifecycle:

**Job Opening → Candidate Application → AI Screening → Interview → Hiring → Employee Onboarding**

The system integrates a **React** web application (HR/Admin), **Flutter** mobile application (Candidate/Employee), **ASP.NET Core** REST API, **PostgreSQL** database, and an **Agentic AI** subsystem powered by **LangGraph + Gemini**.

## 🏗️ Architecture

```
┌─────────────────────┐        ┌───────────────────┐
│   React Web App     │        │  Flutter Mobile    │
│  (HR / Admin)       │        │  (Candidate/       │
│                     │        │   Employee)        │
└─────────┬───────────┘        └─────────┬─────────┘
          │ HTTPS + JWT                  │ HTTPS + JWT
          └──────────┬───────────────────┘
                     ▼
          ┌──────────────────────┐
          │  ASP.NET Core API    │
          │  (.NET 8 / C#)      │
          │  Auth • Business     │
          │  Logic • Validation  │
          └────┬─────────┬──────┘
               │         │
      EF Core  │         │ Internal HTTP
               ▼         ▼
    ┌──────────────┐  ┌─────────────────┐
    │ PostgreSQL   │  │ Agentic AI      │
    │ Database     │  │ Python + FastAPI │
    │              │  │ LangGraph +     │
    │              │  │ Gemini          │
    └──────────────┘  └────────┬────────┘
                               │
                    ┌──────────┼──────────┐
                    ▼          ▼          ▼
              Google Cal   Cloudinary   Email
```

## 👥 User Roles

| Role | Primary Platform | Responsibilities |
|------|-----------------|------------------|
| **System Admin** | React | Manage companies, users, configuration |
| **Recruiter / HR** | React | Create vacancies, manage recruitment |
| **Hiring Manager** | React + Flutter | Review AI recommendations, approve decisions |
| **Candidate** | Flutter | Browse jobs, apply, upload CV, track status |
| **Employee** | Flutter | Complete onboarding, view profile |

## 🧩 Business Components

| Student | Component | AI Agent |
|---------|-----------|----------|
| Student 1 | Company & Job Management | Coordinator Agent |
| Student 2 | Candidate & Application Management | Candidate Analysis Agent |
| Student 3 | Interview & Hiring Management | Interview Scheduling Agent |
| Student 4 | Employee & Onboarding Management | Validation/Safety Agent |

## 🛠️ Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core Web API (.NET 8) |
| ORM | Entity Framework Core + Npgsql |
| Database | PostgreSQL 16 |
| Auth | ASP.NET Identity + JWT |
| Validation | FluentValidation |
| Logging | Serilog |
| API Docs | Swagger / OpenAPI |
| Web App | React + TypeScript + Vite |
| Web State | Zustand + TanStack Query |
| Web Forms | React Hook Form + Zod |
| Mobile App | Flutter + Dart |
| Mobile State | Riverpod |
| Mobile HTTP | Dio |
| Mobile Routing | go_router |
| AI Service | Python 3.13 + FastAPI |
| AI Orchestration | LangGraph |
| LLM | Gemini API |
| AI Validation | Pydantic |
| Backend Tests | xUnit + Moq + FluentAssertions |
| React Tests | Vitest + React Testing Library |
| Flutter Tests | flutter_test + Mocktail |
| AI Tests | pytest |
| Performance | k6 |
| CI/CD | GitHub Actions |
| Infrastructure | Docker Compose |

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- Flutter SDK (stable)
- Python 3.11+
- PostgreSQL 16
- Docker & Docker Compose
- Git

### Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/sishanhewa/SE3090_SE012.git
cd SE3090_SE012

# 2. Start PostgreSQL via Docker Compose
# Note: The database runs on port 5433 (mapped from 5432) to avoid conflicts with local Postgres installs.
# It uses user: talentflow_user | password: talentflow_dev_password
docker-compose -f infra/docker-compose.dev.yml up -d

# 3. Backend
cd backend/src/TalentFlow.Api
dotnet restore
# Apply Entity Framework Core migrations to generate the database
dotnet ef database update --project ../TalentFlow.Infrastructure --startup-project .
# Run the API
dotnet run

# 4. React (in a new terminal)
cd frontend/react-app
npm install
npm run dev

# 5. Flutter (in a new terminal)
cd frontend/flutter-app
flutter pub get
flutter run
# For a physical device, pass the API URL reachable from that device:
# flutter run --dart-define=API_BASE_URL=http://YOUR_COMPUTER_IP:5155/api

# 6. AI Service (in a new terminal)
cd agentic-ai
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

### API Documentation

After starting the backend, visit:
- **Swagger UI**: http://localhost:5155/swagger (development launch profile)

### CV screening workflow

The candidate selects a text-based PDF or DOCX CV (up to 10 MB) in the Flutter application. The application is created first, then the CV is uploaded and linked to that application. If the upload fails, the apply screen offers a retry without submitting a duplicate application. A recruiter can start screening only after a CV is linked.

The internal AI service fetches the linked CV through the authorized ASP.NET API. It extracts readable text, checks job requirements against that text, records its plan, tool calls, validation results and evidence summary, then pauses for recruiter or manager review. The reviewer can shortlist, reject with a reason, or request a new run. Scanned image PDFs without selectable text fail with a clear error and require a text-based replacement.

### Hiring stages and integrations

`Submitted → Screening → Shortlisted or Rejected → Interview → Offered → Hired → Onboarding → Active`. The AI recommendation is evidence for a human screening decision. Shortlisting allocates one future interview slot and places it in the Interviews tab. The invitation is marked **Scheduled** only after the Calendar free/busy check and event creation succeed; without working Calendar credentials it remains **Proposed** with an invitation pending label. Staff can retry from the interview page. A completed interview and feedback are required before drafting an offer. The offer moves through draft, approval, and sent stages; the candidate can accept or decline in the Flutter application. Acceptance creates the employee and onboarding record once. Mandatory onboarding tasks must be completed before an employee becomes Active.

Automatic fallback slots use working days from 09:00 to 17:00 in `Scheduling__TimeZoneId` (default `Asia/Colombo` in the sample configuration). The candidate's external calendar is not accessible; the slot remains tentative until they accept the invitation.

Google Calendar supports an organizer Google account through OAuth or a delegated Workspace service account. For OAuth, create a Google Cloud OAuth client, enable the Calendar API, obtain the organizer's offline refresh token with the `https://www.googleapis.com/auth/calendar.events` and `https://www.googleapis.com/auth/calendar.events.freebusy` scopes, and set `GoogleCalendar__OAuthClientId`, `GoogleCalendar__OAuthClientSecret`, `GoogleCalendar__OAuthRefreshToken`, and `GoogleCalendar__CalendarId` as backend environment secrets. The organizer must have write access to that calendar; `primary` uses the authorized account's primary calendar. For Workspace delegation instead, set `GoogleCalendar__ServiceAccountEmail`, `GoogleCalendar__PrivateKey`, `GoogleCalendar__DelegatedUserEmail`, and `GoogleCalendar__CalendarId`. Never commit credentials or refresh tokens. Calendar events include the candidate as an attendee and use `sendUpdates=all`; failed API calls remain visible as pending invitations. See [Google's OAuth setup](https://developers.google.com/identity/protocols/oauth2/web-server), [event creation](https://developers.google.com/workspace/calendar/api/guides/create-events), and [attendee invitation guide](https://developers.google.com/workspace/calendar/api/concepts/inviting-attendees-to-events).

For hiring confirmation email, set `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl`, `Smtp__FromAddress`, `Smtp__Username`, and `Smtp__Password` through environment secrets. When SMTP is unavailable, the candidate's application still shows **Hired** and the application history records that email delivery is pending. The offer's **Sent** state makes it available in the candidate's application view; it does not claim to have emailed the offer.

For local development, the API also loads `backend/src/TalentFlow.Api/appsettings.Development.local.json` when present. That file is Git-ignored; keep it readable only by your user account and put SMTP credentials there rather than in tracked settings. Gmail SMTP uses `smtp.gmail.com`, port `587`, TLS enabled, the full Gmail address as username, and an app password. Production deployments should use environment secrets.

Flutter uses `10.0.2.2` for the Android emulator and `localhost` for iOS simulator or web. Use `API_BASE_URL` for a physical device or another backend host.

### Production setup

Demo accounts and sample records are created only in the Development environment. For a deployment, apply reviewed EF Core migrations as a release step and provision the first administrator through a controlled process. Public registration creates Candidate accounts only.

### Test Accounts

The following test accounts are seeded when the API starts in Development:

| Role | Email | Password |
|------|-------|----------|
| System Admin | admin@talentflow.com | Admin@123456 |
| Recruiter | recruiter@technova.com | Recruiter@123 |
| Hiring Manager | (Not yet seeded) | |
| Candidate | candidate@example.com | Candidate@123 |

## 🧪 Running Tests

```bash
# Backend tests
cd backend && dotnet test

# React tests
cd frontend/react-app && npm test

# Flutter tests
cd frontend/flutter-app && flutter test

# AI tests
cd agentic-ai && pytest

# Performance tests
cd performance && k6 run k6/auth-load-test.js
```

## 📁 Repository Structure

```
SE3090_SE012/
├── .github/workflows/    # CI/CD pipelines
├── backend/              # ASP.NET Core Web API
├── frontend/
│   ├── react-app/        # React HR/Admin dashboard
│   └── flutter-app/      # Flutter candidate/employee app
├── agentic-ai/           # Python AI microservice
├── docs/                 # ADRs, diagrams, reports
├── infra/                # Docker Compose files
├── performance/          # k6 load tests
└── README.md
```

## 📜 License

This project is developed for academic purposes as part of the SE3090 module at SLIIT.

## 👨‍💻 Team

- **Student 1** — Company & Job Management
- **Student 2 (Lead)** — Candidate & Application Management
- **Student 3** — Interview & Hiring Management
- **Student 4** — Employee & Onboarding Management

## Deployment

The React frontend is deployed from `frontend/react-app` on Vercel. Set its
`VITE_API_BASE_URL` to the public Azure API URL ending in `/api`. The SPA
fallback is in `frontend/react-app/vercel.json`.

The ASP.NET API and FastAPI AI service run on Azure. Set `Frontend__Origin` on
the API to the Vercel frontend origin, `AI_SERVICE_URL` to the AI service URL,
and `BACKEND_SERVICE_URL` on the AI service to the API service root. Configure
a production PostgreSQL connection and a strong JWT key through Azure app
settings; never commit them to the repository.
