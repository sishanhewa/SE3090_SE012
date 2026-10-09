# SE3110 TalentFlow verified submission

Verification date: 9 October 2026. The final editable report has 105 pages, native Word contents/figure/table fields, 34 numbered figures and 84 numbered tables. It follows the supplied reference report structure and documents the actual TalentFlow SE3090 system.

Final local execution: backend 59, React 25, Flutter 16, AI 52, application/security API 22, onboarding API 26, PostgreSQL 5. All 205 passed within the stated scope. k6 repeated checks and ZAP rules are separate. Backend coverage is 14.36% lines (1663/11580) and 15.24% branches (191/1253).

## Reproduce

Use the disposable PostgreSQL database talentflow_se3110_verification on local port 5433. Set ConnectionStrings__DefaultConnection and ASPNETCORE_ENVIRONMENT=Development for the API. Start the API on 5155; its development seeder applies migrations and synthetic fixtures. The React preview was on 5178 and Flutter web preview on 5188.

```sh
dotnet test backend/tests/TalentFlow.UnitTests --configuration Release --logger 'trx;LogFileName=backend.trx' --collect:'XPlat Code Coverage'
(cd frontend/react-app && npm ci && npm run test -- --run --reporter=verbose)
(cd frontend/flutter_app && flutter pub get && flutter test --reporter expanded)
(cd agentic-ai && pip install -r requirements.txt && pytest tests/ -v)
python docs/testing/scripts/live_api_tests.py --base-url http://localhost:5155/api --output live-api.json
python docs/testing/scripts/onboarding_api_tests.py --base-url http://localhost:5155/api --output onboarding-api.json
docker exec -i talentflow-postgres psql -U talentflow_user -d talentflow_se3110_verification < docs/testing/scripts/database_checks.sql
k6 run -e BASE_URL=http://localhost:5155 -e SUMMARY_PATH=load-summary.json performance/k6/load_test_api.js
k6 run -e BASE_URL=http://localhost:5155 -e SUMMARY_PATH=stress-summary.json performance/k6/stress_test_auth.js
```

TEST_ADMIN_EMAIL and TEST_ADMIN_PASSWORD override the seeded development account. JWTs are omitted from JSON exports. Only synthetic fixtures are used. API scripts retain their synthetic records for persisted-state verification; SQL constraint tests roll back.

## Security and performance tools

Official k6 2.3.0 was installed locally for this task. The official ZAP 2.17.0 stable Docker image was downloaded and executed. No additional download is needed on this Mac. Reproduction requires the same tools or compatible versions.

The load run reached 50 virtual users and 4795 requests; p95 59.02 ms and zero failed HTTP requests. The stress run reached 200 virtual users and 28328 requests; p95 892.00 ms and zero failed HTTP requests. Configured thresholds passed, but a failure breakpoint and production capacity were not established.

The unauthenticated passive ZAP web scan reports 3 medium, 5 low and 2 informational alert variants. The public API scan reports 2 low and 1 informational alert. Open header/SRI findings are retained in the report. This is not authenticated active penetration testing. Raw unmodified reports and scan logs are in evidence/zap.

```sh
docker run --rm -v "$PWD/docs/testing/evidence/zap:/zap/wrk/:rw" ghcr.io/zaproxy/zaproxy:stable zap-baseline.py -t http://host.docker.internal:5178 -r web-report.html -J web-report.json -w web-report.md -I
docker run --rm -v "$PWD/docs/testing/evidence/zap:/zap/wrk/:rw" ghcr.io/zaproxy/zaproxy:stable zap-baseline.py -t 'http://host.docker.internal:5155/api/jobs?page=1&pageSize=10' -r api-report.html -J api-report.json -w api-report.md -I
```

For the Docker web scan, start Vite with __VITE_ADDITIONAL_SERVER_ALLOWED_HOSTS=host.docker.internal. This permits that explicit local hostname; authentication and security checks remain enabled. ZAP -I suppresses warning-related exit failure; it does not resolve alerts.

## Evidence and source boundaries

The exact tested local composite is supplied in verification-source/. Member branches are separately retained in testing-branches.bundle. The original Desktop checkout was preserved; its uncommitted deletion of CV helpers prevented AI collection, so the verification retained the committed CV implementation.

The local snapshot includes repaired employee UTC dates, the duplicate task route removal, history insertion fixes, production Flutter widget tests, and production AI validator tests. The onboarding success uses an independently created employee; the full approved screening-to-hire path and external Calendar/Gemini behavior are not claimed.

Raw TRX/XML/JSON/logs, the complete case CSV, defect CSV and cursor-free browser screenshots support the report. Native GitHub screenshots retain each run's actual SHA and outcome. Remote AI still fails pending the responsible member branch. See delivery-status.md for exact local and remote delivery.

Each member must personally review and demonstrate assigned work and declare AI assistance using the report's CLEAR section. The real verification date is after the assignment deadline and is recorded accurately.

## Responsibilities and acceptance

Rosa: company/jobs and performance. Hewapathirana: candidate/applications, React and shared live API/database evidence. Madhushan: interview/hiring and Flutter widget tests. Dewmini: employee/onboarding and AI scoring/prompt safety. Exact authors and branches follow priority_instructions.md. All selected functional assertions and performance thresholds must pass; open security findings, limited coverage and incomplete remote delivery remain documented.
