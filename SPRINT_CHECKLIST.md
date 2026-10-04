# 🚀 TalentFlow — Sprint Progress Checklist

> **Project:** TalentFlow — Intelligent Recruitment & Employee Management Platform  
> **Team:** SE3090_SE012 (4 Members)  
> **Last Updated:** 2026-09-27  
> **Current Stage:** 🟢 **Sprint 3 In Progress — Core AI Implementation Done**

---

## 📊 Overall Progress Summary

| Sprint | Status | Completion |
|--------|--------|------------|
| Sprint 1 — Foundation & Architecture | ✅ Complete | ~95% |
| Sprint 2 — Core Business Features | 🟡 Mostly Complete | ~85% |
| Sprint 3 — Agentic AI + Integration | 🟢 Core Done | ~65% (agents, tools, graph, approval) |
| Sprint 4 — Testing, Deployment, Docs | 🟡 In Progress | ~25% (unit + AI tests added) |

---

## 🏗️ SPRINT 1 — Foundation & Architecture (✅ COMPLETE)

### Shared / Team-Wide

- [x] GitHub repository created
- [x] `develop` branch created
- [x] Feature branches created (`s1-company-jobs`, `s2-candidates-apps`, `s3-interviews-hiring`, `s4-employees-onboarding`)
- [x] PR template created (`.github/pull_request_template.md`)
- [x] Issue templates created (`.github/ISSUE_TEMPLATE/`)
- [x] CI pipelines set up (`backend-ci.yml`, `react-ci.yml`, `flutter-ci.yml`, `ai-ci.yml`)
- [x] Docker Compose configured (`docker-compose.yml`, `docker-compose.dev.yml`)
- [x] `.env.example` created
- [x] `.gitignore` configured
- [x] Clean Architecture backend structure (Domain → Application → Infrastructure → Api)
- [x] JWT Authentication with ASP.NET Identity (`AuthController`, `AuthService`)
- [x] User roles defined (`UserRole` enum: Admin, Recruiter, HiringManager, Interviewer, Candidate)
- [x] CORS configured
- [x] Swagger/OpenAPI configured
- [x] PostgreSQL database connection
- [x] EF Core initial migration (`20260914061956_InitialCreate`)
- [x] Data seeder created (`DataSeeder.cs`)
- [x] ADRs written (5 total: React state, Flutter state, AI framework, workflow persistence, cloud deployment)
- [x] React dashboard scaffolded with routing, auth store, dark theme
- [x] Flutter app scaffolded with auth and job list foundation
- [x] Agentic AI Python project scaffolded (FastAPI + agents directory)

### Member 1 — Company & Job Management (Sprint 1)

- [x] `Company` entity created
- [x] `Department` entity created
- [x] `Job` entity created
- [x] `JobRequirement` entity created
- [x] `Skill` entity created
- [x] `CompanyMembership` entity created
- [x] `JobStatus` enum (Draft, Published, Closed, Archived)
- [x] `CompanyConfiguration` (EF Core fluent config)
- [x] `JobConfiguration` (EF Core fluent config)
- [x] `CompanyRepository` created
- [x] `JobRepository` created
- [x] `ICompanyRepository` interface
- [x] `IJobRepository` interface
- [x] `CompanyService` created
- [x] `JobService` created
- [x] `CompaniesController` created
- [x] `JobsController` created
- [x] Company & Job DTOs created
- [x] Coordinator Agent defined (schema + allowed tools)
- [x] Coordinator Agent input/output schema not formalized in shared docs

### Member 2 — Candidate & Application Management (Sprint 1)

- [x] `CandidateProfile` entity created
- [x] `Application` entity created
- [x] `ApplicationStatus` enum (Submitted, Screening, Shortlisted, Interview, Offered, Hired, Rejected, Withdrawn)
- [x] `CandidateConfiguration` (EF Core fluent config)
- [x] `ApplicationConfiguration` (EF Core fluent config)
- [x] `ApplicationRepository` created
- [x] `CandidateProfileService` created
- [x] `ApplicationService` created
- [x] `CandidateProfilesController` created
- [x] `ApplicationsController` created
- [x] Candidate & Application DTOs created
- [x] Candidate Analysis Agent defined (schema + allowed tools + denied tools)
- [x] `CandidateSkill`, `CandidateEducation`, `CandidateExperience` — verified as separate entities
- [ ] `CandidateDocument` entity — document upload investigation needed

### Member 3 — Interview & Hiring Management (Sprint 1)

- [x] `Interview` entity created
- [x] `InterviewStatus` enum
- [x] `InterviewConfiguration` (EF Core fluent config)
- [x] `InterviewRepository` created
- [x] `InterviewService` created
- [x] `InterviewsController` created
- [x] Interview DTOs created
- [x] Interview Agent defined (schema + allowed tools)
- [x] `InterviewPanelMember` — verified separate entity exists
- [x] `InterviewFeedback` — verified structure
- [x] `HiringDecision` entity — verified separate entity exists
- [ ] Google Calendar API spike not documented

### Member 4 — Employee & Onboarding Management (Sprint 1)

- [x] `Employee` entity created
- [x] `EmployeeStatus` enum
- [x] `EmployeeConfiguration` (EF Core fluent config)
- [x] `EmployeeRepository` created
- [x] `EmployeeService` created
- [x] `EmployeesController` created
- [x] Employee DTOs created
- [x] Validation Agent defined (schema + allowed tools)
- [x] `WorkflowExecution` entity created (shared AI DB table)
- [x] `WorkflowConfiguration` (EF Core fluent config)
- [x] `WorkflowsController` scaffolded
- [x] `EmployeePosition` entity — using string Position instead
- [x] `OnboardingTemplate` entity — created
- [x] `OnboardingTask` entity — created
- [x] `EmployeeOnboardingTask` entity — created
- [x] `EmployeeStatusHistory` entity — created
- [x] `AgentStep`, `ToolCall`, `ValidationResult`, `WorkflowApproval` DB tables — created

### Sprint 1 Checkpoint

- [x] Login/register works
- [x] JWT works
- [x] Roles defined
- [x] React can call API
- [x] Flutter can call API
- [x] PostgreSQL migration works
- [x] Swagger works
- [x] CI builds (4 pipelines)
- [ ] Full role-based route protection needs testing

---

## 🔧 SPRINT 2 — Core Business Features (🟡 ~85% COMPLETE)

### Member 1 — Complete Job Management (Sprint 2)

**Backend:**
- [x] `POST /jobs` — Create job
- [x] `GET /jobs` — List jobs
- [x] `GET /jobs/{id}` — Job details
- [x] `PUT /jobs/{id}` — Edit job
- [x] `POST /jobs/{id}/publish` — Publish job
- [x] `POST /jobs/{id}/close` — Close job
- [x] Archive transition implemented
- [x] Search/filter support
- [x] Publication validation (title, description, department, requirements, vacancy, deadline)
- [x] Deadline validation (must be in future)
- [ ] Vacancy limit enforcement — verify
- [ ] Company isolation thoroughness — verify

**React:**
- [x] Job List page
- [x] Create Job page
- [x] Edit Job page
- [x] Job Details page
- [x] Applications link from job
- [x] Dashboard with live API stats

**Flutter:**
- [x] Job Search (list with search)
- [x] Job Details screen
- [x] Filters
- [ ] Sort options — verify

**Tests:**
- [x] `JobServiceTests.cs` exists (basic)
- [ ] Comprehensive tests: publish invalid job, apply to closed job, company isolation

### Member 2 — Complete Candidate/Application Management (Sprint 2)

**Backend:**
- [x] Application creation endpoint
- [x] Application listing/details
- [x] Application withdraw support
- [x] Application deadline validation
- [x] Application status transitions
- [x] Duplicate application prevention
- [x] `POST /applications/{id}/documents` — Document upload endpoint
- [x] Application history tracking — `ApplicationHistory` entity

**React:**
- [x] Applicant List page
- [x] Application Details page
- [x] Candidate Profile page
- [ ] CV viewer
- [ ] Application history timeline

**Flutter:**
- [x] Candidate Profile screen
- [x] Applications List (My Applications)
- [x] Apply to Job flow
- [x] Navigation integrated
- [ ] CV Upload (file_picker)
- [ ] Application Status tracking

**Tests:**
- [x] `ApplicationServiceTests.cs` exists (basic)
- [ ] Tests for: duplicate application, expired vacancy, invalid transition, document validation

### Member 3 — Complete Interview/Hiring Management (Sprint 2)

**Backend:**
- [x] `POST /interviews` — Create interview
- [x] `GET /interviews` — List interviews
- [x] `GET /interviews/{id}` — Interview details
- [x] `POST /interviews/{id}/feedback` — Submit feedback
- [x] `POST /interviews/{id}/complete` — Complete interview
- [x] Interview scheduling with status updates
- [x] Application status validation for scheduling
- [x] Offer management (Controller, Service, Repository)
- [x] `OfferStatus` enum
- [x] Candidate conflict detection — verified
- [x] Interviewer conflict detection — verified
- [x] Shortlisted-only rule enforcement — verified
- [ ] Google Calendar basic integration — not started

**React:**
- [x] Interview List/Calendar page
- [x] Interview Details page
- [x] Offers page
- [x] Feedback Form — verified
- [x] Hiring Decision UI — verified

**Flutter:**
- [x] Interview Detail screen
- [x] url_launcher integration
- [ ] Candidate interview list — verify
- [ ] Offer view/accept/reject — verify

**Tests:**
- [x] `InterviewServiceTests.cs` exists (basic)
- [ ] Tests for: conflict detection, shortlisted-only, calendar failure

### Member 4 — Complete Employee/Onboarding Management (Sprint 2)

**Backend:**
- [x] `GET /employees` — List employees
- [x] `GET /employees/{id}` — Employee details
- [x] `PATCH /employees/{id}/status` — Status transitions
- [x] Employee service with business logic
- [ ] `POST /employees/from-hire/{applicationId}` — Employee creation from accepted offer
- [x] `GET /employees/{id}/onboarding` — Onboarding tasks endpoint
- [x] `PATCH /onboarding/tasks/{id}` — Task completion
- [ ] Onboarding template auto-creation

**React:**
- [x] Employee List page
- [x] Employee Details page
- [x] Onboarding Management page
- [ ] Onboarding task management — may be UI-only without backend

**Flutter:**
- [ ] Employee Profile screen — verify
- [ ] Onboarding Checklist — not confirmed

**Tests:**
- [x] `EmployeeServiceTests.cs` exists (basic)
- [ ] Tests for: status transitions, onboarding completion, company isolation

### Sprint 2 Checkpoint — Manual E2E Verification

> **Goal:** Without AI, demonstrate the full manual recruitment flow.

- [ ] Recruiter creates job in React
- [ ] Candidate sees job in Flutter
- [ ] Candidate applies
- [ ] Recruiter sees application in React
- [ ] Recruiter manually creates interview
- [ ] Candidate sees interview in Flutter
- [ ] Interview → Feedback → Offer → Accept → Employee flow

> **This checkpoint must be verified before proceeding to Sprint 3.**

---

## 🤖 SPRINT 3 — Agentic AI + Cross-Platform Integration (🔴 ~5% — SCAFFOLD ONLY)

> **This is the most critical sprint.** The assignment's highest-weighted deliverable is the multi-agent AI workflow with cross-platform integration.

### Member 1 — Coordinator Agent (Sprint 3)

- [x] Implement Gemini API client (Python)
- [x] Implement structured plan generation (Gemini → JSON plan)
- [x] Implement LangGraph coordinator node
- [x] Implement delegation logic (route steps to correct agent)
- [x] Implement workflow status handling (Planning → InProgress → AwaitingApproval)
- [x] Implement tools:
  - [x] `create_plan` — LLM generates structured multi-step plan
  - [x] `delegate_step` — dispatch step to target agent
  - [x] `compile_results` — aggregate agent outputs into final result
- [x] ASP.NET integration:
  - [x] `POST /api/workflows/recruitment-screening` — trigger full workflow
  - [x] `GET /api/workflows/{id}` — get workflow status & results
- [x] Test: plan generation with valid input
- [x] Test: plan generation with edge cases

### Member 2 — Candidate Analysis Agent (Sprint 3)

- [x] Implement tool functions (calling ASP.NET backend API):
  - [x] `get_job_requirements` — fetch job skills/requirements
  - [x] `get_candidate_profile` — fetch candidate profile data
  - [x] `get_candidate_skills` — fetch candidate skills
  - [x] `get_candidate_experience` — fetch candidate experience
  - [x] `get_application_documents` — fetch uploaded documents/CV
- [x] Implement LLM-based analysis using Gemini
- [x] Implement structured output schema (matched skills, missing skills, experience, qualifications, evidence)
- [x] Implement **deterministic scoring** (backend calculation, NOT LLM-decided):
  - [x] Mandatory skills score (e.g., 40 points)
  - [x] Preferred skills score (e.g., 20 points)
  - [x] Experience score (e.g., 25 points)
  - [x] Education score (e.g., 10 points)
  - [x] Certification score (e.g., 5 points)
  - [x] Threshold rules: ≥75 → interview, 60-74 → manual review, <60 → not recommended
- [x] Test: good candidate (high score)
- [x] Test: weak candidate (low score)
- [x] Test: missing CV
- [x] Test: invalid/expired application

### Member 3 — Interview Agent (Sprint 3)

- [x] Implement availability tools:
  - [x] `get_candidate_availability` — check candidate schedule
  - [x] `get_interviewer_availability` — check interviewer schedule
  - [x] `get_calendar_availability` — check Google Calendar
- [x] Implement slot suggestion logic (propose 3 options)
- [x] Implement `create_interview_draft` — draft interview (pending approval)
- [x] After approval:
  - [x] Create actual interview record in DB
  - [ ] Create Google Calendar event (optional)
  - [x] Update application status to "Interview"
- [x] Test: calendar timeout handling
- [x] Test: scheduling collision detection
- [x] Test: invalid slot proposal

### Member 4 — Validation Agent + Workflow State (Sprint 3)

- [x] Implement Pydantic schema validation
- [x] Implement business rule checks:
  - [x] Application belongs to requested job
  - [x] Application status is valid for operation
  - [x] Candidate meets mandatory requirements
  - [x] Scoring calculation matches configured weights
  - [x] Proposed slots have no collisions
  - [x] Requesting user is authorized
- [x] Implement tool output schema validation
- [x] Implement workflow persistence:
  - [x] Create `AgentStep` DB table/entity
  - [x] Create `ToolCall` DB table/entity
  - [x] Create `ValidationResult` DB table/entity
  - [x] Create `WorkflowApproval` DB table/entity
  - [x] Persist each step execution with timestamps
- [x] Implement approval API endpoints:
  - [x] `POST /workflows/{id}/approve`
  - [x] `POST /workflows/{id}/reject`
  - [x] `POST /workflows/{id}/revise`
- [x] Security: approval authorization + company isolation

### Shared — React AI Workflow UI (Sprint 3)

- [x] AI Workflows list page
- [x] Pending Approvals page (for Hiring Managers) (Included in Workflows list)
- [x] Workflow Details page showing:
  - [x] Execution Timeline
  - [x] Agent Steps with status
  - [x] Tool Calls log
  - [x] Validation Results
  - [x] Approval History
  - [x] Errors/Warnings
- [x] Approval action buttons: [Approve] [Reject] [Request Revision]
- [x] Candidate evaluation summary (score, recommendation, warnings)

### Shared — Flutter AI Status UI (Sprint 3)

- [x] AI Screening Status indicator on application
- [x] Application Workflow Progress tracker
- [x] Interview result/status updates
- [ ] Real-time or polling-based status refresh

### Sprint 3 Checkpoint — THE CRITICAL MILESTONE 🎯

> Run this end-to-end live:

- [ ] 1. Candidate applies in Flutter
- [ ] 2. Recruiter starts AI screening in React
- [ ] 3. Coordinator Agent creates structured plan (via Gemini)
- [ ] 4. Candidate Analysis Agent evaluates candidate (tools + LLM)
- [ ] 5. Deterministic score calculated (backend business logic)
- [ ] 6. Validation Agent checks business rules
- [ ] 7. Interview Agent proposes interview slots
- [ ] 8. Workflow state persisted to DB
- [ ] 9. Workflow pauses at AwaitingApproval
- [ ] 10. Hiring Manager opens React → sees pending approval
- [ ] 11. Manager approves → workflow resumes
- [ ] 12. Interview created in DB (+ optional Calendar event)
- [ ] 13. Candidate Flutter app shows updated status

> **If this works reliably, the most complex integration requirement is covered.**

---

## 🧪 SPRINT 4 — Testing, Deployment, Documentation & Viva (🔴 ~10%)

### Member 1 — Tests & Docs (Sprint 4)

- [ ] Comprehensive Job Service tests
- [ ] Job Controller tests
- [ ] Job business rules tests
- [ ] Coordinator Agent tests
- [ ] Documentation:
  - [ ] Component 1 individual report
  - [ ] Coordinator Agent architecture
  - [ ] Job API documentation
  - [ ] ADR contribution
- [ ] Viva preparation: REST API, Coordinator, CI

### Member 2 — Tests & Docs (Sprint 4)

- [ ] Application Service tests (comprehensive)
- [ ] Candidate validation tests
- [ ] File upload tests
- [ ] Candidate Analysis Agent tests
- [ ] AI evaluation:
  - [ ] Golden test cases
  - [ ] Score validation accuracy
  - [ ] Bad structured output handling
- [ ] Documentation:
  - [ ] Candidate/Application section
  - [ ] AI Agent section
  - [ ] Individual report

### Member 3 — Tests & Docs (Sprint 4)

- [ ] Interview conflict detection tests
- [ ] Calendar failure handling tests
- [ ] Offer workflow tests
- [ ] Interview Agent tests
- [ ] Performance/integration:
  - [ ] Calendar API latency tests
  - [ ] Interview API load tests
- [ ] Documentation:
  - [ ] Interview/Hiring section
  - [ ] Third-party integration section
  - [ ] Individual report

### Member 4 — Tests & Docs (Sprint 4)

- [ ] Employee conversion tests
- [ ] Onboarding task completion tests
- [ ] Validation Agent tests
- [ ] Approval security tests
- [ ] Workflow state persistence tests
- [ ] AI security tests:
  - [ ] Prompt injection handling
  - [ ] Unauthorized approval attempt
  - [ ] Tool failure graceful handling
  - [ ] Invalid schema rejection
  - [ ] Safe failure modes
- [ ] Documentation:
  - [ ] AI evaluation report
  - [ ] Workflow persistence docs
  - [ ] Security docs
  - [ ] Individual report

### Shared — E2E Testing (Sprint 4)

- [ ] Automated E2E scenario: Application → AI → Approval → Interview
- [ ] Manual E2E walkthrough documented

### Shared — Performance Testing (Sprint 4)

- [ ] k6 performance test scripts
- [ ] k6 test results documented
- [ ] API endpoint load testing
- [ ] AI workflow latency benchmarks

### Shared — Deployment (Sprint 4)

- [ ] ASP.NET Core deployment (Docker)
- [ ] PostgreSQL deployment
- [ ] React build & deployment
- [ ] AI service deployment
- [ ] Flutter APK build
- [ ] Deployment documentation

### Shared — Final Documentation (Sprint 4)

- [ ] README.md updated and finalized
- [ ] ER diagram
- [ ] Architecture diagram
- [ ] Agent architecture diagram
- [ ] Sequence diagram (full workflow)
- [ ] Testing report
- [ ] AI evaluation report
- [ ] Deployment report
- [ ] All ADRs finalized
- [ ] Individual reports (one per member)
- [ ] AI usage logs
- [ ] API documentation

### Demo Preparation (Sprint 4)

- [ ] 10-minute demonstration script
- [ ] Role-based login demo
- [ ] CRUD/business logic demo
- [ ] React + Flutter shared API usage demo
- [ ] Complete Agentic AI workflow demo
- [ ] Tests & CI demo
- [ ] Deployment demo
- [ ] Rehearse viva Q&A

---

## 🔥 Priority Actions — What To Do Next

### Immediate (Before Starting Sprint 3)

1. **Verify Sprint 2 checkpoint** — Run the manual recruitment flow end-to-end without AI
2. **Create missing entities** — `OnboardingTemplate`, `OnboardingTask`, `EmployeeOnboardingTask`, `EmployeeStatusHistory`, `EmployeePosition`
3. **Create missing AI DB entities** — `AgentStep`, `ToolCall`, `ValidationResult`, `WorkflowApproval`
4. **Verify document upload** — Ensure CV/file upload works in Flutter + backend
5. **Verify interview conflict detection** — Ensure no overlapping interviews

### Sprint 3 Execution Order (Dependency-Aware)

```
Week 1:
├── M4: Create AI workflow DB tables (AgentStep, ToolCall, etc.)
├── M1: Implement Gemini client + Coordinator plan generation
├── M2: Implement Candidate Analysis tools (API calls)
└── M3: Implement availability check tools

Week 2:
├── M2: Implement deterministic scoring in backend
├── M4: Implement Validation Agent + workflow persistence
├── M1: Implement delegation + workflow status management
├── M3: Implement interview draft creation
├── ALL: Build React approval UI
└── ALL: Build Flutter status tracking UI

Integration:
├── Connect Coordinator → CandidateAnalysis → Validation → Interview flow
├── Wire up approval endpoints
├── Test full E2E: Flutter → API → AI → React Approval → Flutter
└── Debug and stabilize
```

### Critical Path Items

| Priority | Item | Owner | Why Critical |
|----------|------|-------|-------------|
| 🔴 P0 | Gemini API client + LangGraph setup | M1 | Everything depends on this |
| 🔴 P0 | AI workflow DB tables | M4 | Persistence required for all agents |
| 🔴 P0 | Candidate Analysis tools | M2 | Core of AI screening |
| 🔴 P0 | Deterministic scoring | M2 | Assignment explicitly requires this |
| 🟡 P1 | Approval API endpoints | M4 | Required for human-in-the-loop |
| 🟡 P1 | React approval UI | All | Demo-critical |
| 🟡 P1 | Interview scheduling tools | M3 | Part of the full workflow |
| 🟢 P2 | Flutter status UI | All | Nice for demo but less complex |
| 🟢 P2 | Google Calendar integration | M3 | Optional but valuable |
