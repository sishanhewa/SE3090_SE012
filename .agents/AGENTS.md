# Git Workflow & Commit Rules

**CRITICAL BEHAVIORAL RULE:** This is an academic project and commits must accurately reflect individual contributions. Before making commits, you MUST update the local git configuration (`git config user.name` and `git config user.email`) to match the member doing the work. 

## Team Member Assignments & Git Identities

| Member | Component | Git Name | Git Email | Branch |
|--------|-----------|----------|-----------|--------|
| M1 | Company & Job Management | vihara-rosa | rosavihara@gmail.com | `feature/s1-company-jobs` |
| M2 (Lead) | Candidate & Application Management | sishanhewa | sishanhewa4@gmail.com | `feature/s2-candidates-apps` |
| M3 | Interview & Hiring Management | gaveeshamadhushan | gaveeshamadhushan15@gmail.com | `feature/s3-interviews-hiring` |
| M4 | Employee & Onboarding Management | chenu222 | dewminichethani222@gmail.com | `feature/s4-employees-onboarding` |

## Instructions for Agents
1. **Never commit shared/foundation work under the wrong name.** If you are doing shared work, explicitly ask the user which member should receive the commit credit, or default to the Team Lead (`sishanhewa`).
2. **Shared work goes to `develop` branch** under `sishanhewa`.
3. **Member-specific work goes to their branch.** Use the exact branches listed above — do NOT create new branches.
4. **Verify Git Config before committing.** Run `git config --get user.name` to ensure the correct persona is active before running `git commit`.
5. **Keep member branches up to date.** Before starting work on a member branch, always merge `develop` into it first.
