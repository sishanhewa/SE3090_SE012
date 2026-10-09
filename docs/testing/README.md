# SE3110 TalentFlow verification

Verification date: 9 October 2026. The completed report and evidence describe the actual TalentFlow SE3090 system.

Final local results: backend 56; React 25; Flutter 16; AI 52; live API workflow/security 22; PostgreSQL 5. All 176 passed after repairs. k6 load and stress scenarios also passed all configured thresholds. Backend coverage: 13.70% of instrumented lines and 14.81% of branches. These are scoped checks, not complete application coverage.

## Reproduce

Use a disposable database named talentflow_se3110_verification. Set the API's ConnectionStrings__DefaultConnection to that database and ASPNETCORE_ENVIRONMENT=Development. Start the API on port 5155; the development seeder applies migrations and sample records. Use only synthetic data for the mutation tests.

```sh
dotnet test backend/tests/TalentFlow.UnitTests --logger 'trx;LogFileName=backend.trx' --collect:'XPlat Code Coverage'
(cd frontend/react-app && npm ci && npm run test -- --run --reporter=verbose)
(cd frontend/flutter_app && flutter pub get && flutter test --reporter expanded)
(cd agentic-ai && pip install -r requirements.txt && pytest tests/ -v)
python docs/testing/scripts/live_api_tests.py --base-url http://127.0.0.1:5155/api
docker exec -i talentflow-postgres psql -U talentflow_user -d talentflow_se3110_verification < docs/testing/scripts/database_checks.sql
k6 run -e SUMMARY_PATH=load-summary.json performance/k6/load_test_api.js
k6 run -e SUMMARY_PATH=stress-summary.json performance/k6/stress_test_auth.js
```

TEST_ADMIN_EMAIL and TEST_ADMIN_PASSWORD override the development test account. API test exports omit authentication token values. BASE_URL and SUMMARY_PATH configure k6. Tests must never target production.

## Source and evidence boundaries

The local test snapshot combines the supplied working-tree tests with verified repairs and replacements. Production Flutter widgets and router tests replace the generic samples. AI scoring cases call the production validator rather than copied test-only arithmetic. The application history regression verifies new rows are inserted, and the live PostgreSQL workflow independently retests the withdrawal fix.

Remote CI results apply only to each run's source SHA. See delivery-status.md for remote versus local component delivery. The original Desktop project checkout was preserved because it contains uncommitted changes, including a CV-tool deletion that prevents AI test collection. The verification uses the committed CV retrieval implementation.

The report, complete case register, defect register, screenshots, XML/TRX/JSON outputs and source bundle provide the remaining submission evidence. Each member must review and personally demonstrate their assigned tests and declare AI assistance according to module guidance.
