# Priority Instructions: Git Workflow & Member Assignments

**CRITICAL RULE:** All future work MUST be committed and pushed using the exact GitHub account and email of the member responsible for that component. 

This is an academic project and commits must accurately reflect individual contributions. Before making commits, the AI MUST update the local git configuration (`git config user.name` and `git config user.email`) to match the member doing the work.

## Team Member Assignments & Git Identities

### Member 1: Company & Job Management
- **Name:** vihara-rosa
- **Email:** rosavihara@gmail.com
- **Branch:** `feature/s1-company-jobs` (already exists)

### Member 2: Candidate & Application Management (Lead)
- **Name:** sishanhewa
- **Email:** sishanhewa4@gmail.com
- **Branch:** `feature/s2-candidates-apps` (already exists)

### Member 3: Interview & Hiring Management
- **Name:** gaveeshamadhushan
- **Email:** gaveeshamadhushan15@gmail.com
- **Branch:** `feature/s3-interviews-hiring` (already exists)

### Member 4: Employee & Onboarding Management
- **Name:** chenu222
- **Email:** dewminichethani222@gmail.com
- **Branch:** `feature/s4-employees-onboarding` (already exists)

## Workflow Rules
1. **Never commit shared/foundation work under the wrong name.** If the AI is doing shared work, explicitly ask the user which member should receive the commit credit, or default to the Team Lead (`sishanhewa`).
2. **Shared work goes to `develop` branch** under `sishanhewa`.
3. **Member-specific work goes to their branch.** Each member works on their own branch and creates Pull Requests back into `develop`.
4. **Verify Git Config before committing.** Run `git config --get user.name` to ensure the correct persona is active before running `git commit`.
5. **Keep member branches up to date.** Before starting work on a member branch, always rebase or merge `develop` into it first.
