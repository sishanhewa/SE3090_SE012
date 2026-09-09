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

# 2. Copy environment variables
cp .env.example .env
# Edit .env with your actual values

# 3. Start PostgreSQL (if using Docker)
docker compose -f infra/docker-compose.dev.yml up -d postgres

# 4. Backend
cd backend
dotnet restore
dotnet ef database update --project src/TalentFlow.Infrastructure --startup-project src/TalentFlow.Api
dotnet run --project src/TalentFlow.Api

# 5. React (in a new terminal)
cd frontend/react-app
npm install
npm run dev

# 6. Flutter (in a new terminal)
cd frontend/flutter-app
flutter pub get
flutter run

# 7. AI Service (in a new terminal)
cd agentic-ai
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

### API Documentation

After starting the backend, visit:
- **Swagger UI**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health

### Test Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@talentflow.com | Admin@123 |
| Recruiter | recruiter@talentflow.com | Recruiter@123 |
| Hiring Manager | manager@talentflow.com | Manager@123 |
| Candidate | candidate@talentflow.com | Candidate@123 |

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
