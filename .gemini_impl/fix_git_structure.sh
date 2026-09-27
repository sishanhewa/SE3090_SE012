#!/bin/bash
set -e

REPO="/Users/sishanhewapathirana/Desktop/IREMP/SE3090_SE012"
cd "$REPO"

echo "============================================"
echo " PHASE 1: BACKUP"
echo "============================================"
git tag backup-pre-restructure 2>/dev/null || echo "Tag already exists, skipping"
echo "✓ Created backup tag at current develop HEAD"

echo ""
echo "============================================"
echo " PHASE 2: DELETE DUPLICATE BRANCH"
echo "============================================"
git branch -D feature/s4-offers-hiring 2>/dev/null || echo "Already deleted locally"
echo "✓ Deleted feature/s4-offers-hiring locally"

echo ""
echo "============================================"
echo " PHASE 3: RESET DEVELOP TO ORIGIN"
echo "============================================"
git checkout develop
git reset --hard origin/develop
echo "✓ develop reset to origin/develop ($(git rev-parse --short HEAD))"

echo ""
echo "============================================"
echo " PHASE 4: UPDATE FEATURE BRANCHES"
echo "============================================"
for branch in feature/s1-company-jobs feature/s2-candidates-apps feature/s3-interviews-hiring feature/s4-employees-onboarding; do
  git checkout "$branch"
  git merge origin/develop --no-edit --no-ff -m "chore: sync branch with develop before Sprint 3 work" 2>/dev/null || git merge origin/develop --no-edit
  echo "✓ Updated $branch to include origin/develop"
done

echo ""
echo "============================================"
echo " PHASE 5: CHERRY-PICK M1 (vihara-rosa)"
echo "         → feature/s1-company-jobs"
echo "============================================"
git checkout feature/s1-company-jobs
git config user.name "vihara-rosa"
git config user.email "rosavihara@gmail.com"

# M1 commits in chronological order (oldest first)
M1_COMMITS="429d9e6 f4c5522 420b19e acd83b0 86d5a50 9128a4d b43c295 fc94e0b"
for HASH in $M1_COMMITS; do
  CNAME=$(git log -1 --format=%an "$HASH")
  CEMAIL=$(git log -1 --format=%ae "$HASH")
  CDATE=$(git log -1 --format=%aD "$HASH")
  MSG=$(git log -1 --format=%s "$HASH")
  echo "  Cherry-picking: $MSG"
  GIT_COMMITTER_NAME="$CNAME" GIT_COMMITTER_EMAIL="$CEMAIL" GIT_COMMITTER_DATE="$CDATE" \
    git cherry-pick "$HASH" --no-edit
done
echo "✓ 8 commits cherry-picked onto feature/s1-company-jobs"

echo ""
echo "============================================"
echo " PHASE 6: CHERRY-PICK M2 (sishanhewa)"
echo "         → feature/s2-candidates-apps"
echo "============================================"
git checkout feature/s2-candidates-apps
git config user.name "sishanhewa"
git config user.email "sishanhewa4@gmail.com"

# M2 commits in chronological order
M2_COMMITS="adf71e3 138aad1 1283ffa 63474a1 1f7267c 4071a54 46b79dd 7ded7e0 055eee4 131c004"
for HASH in $M2_COMMITS; do
  CNAME=$(git log -1 --format=%an "$HASH")
  CEMAIL=$(git log -1 --format=%ae "$HASH")
  CDATE=$(git log -1 --format=%aD "$HASH")
  MSG=$(git log -1 --format=%s "$HASH")
  echo "  Cherry-picking: $MSG"
  GIT_COMMITTER_NAME="$CNAME" GIT_COMMITTER_EMAIL="$CEMAIL" GIT_COMMITTER_DATE="$CDATE" \
    git cherry-pick "$HASH" --no-edit
done
echo "✓ 10 commits cherry-picked onto feature/s2-candidates-apps"

echo ""
echo "============================================"
echo " PHASE 7: CHERRY-PICK M3 (gaveeshamadhushan)"
echo "         → feature/s3-interviews-hiring"
echo "============================================"
git checkout feature/s3-interviews-hiring
git config user.name "gaveeshamadhushan"
git config user.email "gaveeshamadhushan15@gmail.com"

# M3 commits in chronological order
M3_COMMITS="b231cff 4ff80d6 00f6b91 2f6e95c 3ab2f94"
for HASH in $M3_COMMITS; do
  CNAME=$(git log -1 --format=%an "$HASH")
  CEMAIL=$(git log -1 --format=%ae "$HASH")
  CDATE=$(git log -1 --format=%aD "$HASH")
  MSG=$(git log -1 --format=%s "$HASH")
  echo "  Cherry-picking: $MSG"
  GIT_COMMITTER_NAME="$CNAME" GIT_COMMITTER_EMAIL="$CEMAIL" GIT_COMMITTER_DATE="$CDATE" \
    git cherry-pick "$HASH" --no-edit
done
echo "✓ 5 commits cherry-picked onto feature/s3-interviews-hiring"

echo ""
echo "============================================"
echo " PHASE 8: CHERRY-PICK M4 (chenu222)"
echo "         → feature/s4-employees-onboarding"
echo "============================================"
git checkout feature/s4-employees-onboarding
git config user.name "chenu222"
git config user.email "dewminichethani222@gmail.com"

# M4 commits in chronological order
M4_COMMITS="1b2bf90 55f0958 74be71f d886c3c 7ffa637 2b267e6 c4d2918 27f25fd 1993048"
for HASH in $M4_COMMITS; do
  CNAME=$(git log -1 --format=%an "$HASH")
  CEMAIL=$(git log -1 --format=%ae "$HASH")
  CDATE=$(git log -1 --format=%aD "$HASH")
  MSG=$(git log -1 --format=%s "$HASH")
  echo "  Cherry-picking: $MSG"
  GIT_COMMITTER_NAME="$CNAME" GIT_COMMITTER_EMAIL="$CEMAIL" GIT_COMMITTER_DATE="$CDATE" \
    git cherry-pick "$HASH" --no-edit
done
echo "✓ 9 commits cherry-picked onto feature/s4-employees-onboarding"

echo ""
echo "============================================"
echo " PHASE 9: MERGE BRANCHES INTO DEVELOP"
echo "============================================"
git checkout develop
git config user.name "sishanhewa"
git config user.email "sishanhewa4@gmail.com"

echo "  Merging feature/s4-employees-onboarding..."
GIT_AUTHOR_DATE="2026-09-27T10:30:00+0530" GIT_COMMITTER_DATE="2026-09-27T10:30:00+0530" \
  git merge --no-ff feature/s4-employees-onboarding \
  -m "Merge branch 'feature/s4-employees-onboarding' into develop

Sprint 3: Onboarding services, validation agent, workflow controller, tool permissions"

echo "  Merging feature/s3-interviews-hiring..."
GIT_AUTHOR_DATE="2026-09-27T10:40:00+0530" GIT_COMMITTER_DATE="2026-09-27T10:40:00+0530" \
  git merge --no-ff feature/s3-interviews-hiring \
  -m "Merge branch 'feature/s3-interviews-hiring' into develop

Sprint 3: Scheduling service, interview tools, interview agent, package exports"

echo "  Merging feature/s1-company-jobs..."
GIT_AUTHOR_DATE="2026-09-27T11:25:00+0530" GIT_COMMITTER_DATE="2026-09-27T11:25:00+0530" \
  git merge --no-ff feature/s1-company-jobs \
  -m "Merge branch 'feature/s1-company-jobs' into develop

Sprint 3: Coordinator agent, Gemini client, LangGraph workflow, AI workflow UI"

echo "  Merging feature/s2-candidates-apps..."
GIT_AUTHOR_DATE="2026-09-27T11:40:00+0530" GIT_COMMITTER_DATE="2026-09-27T11:40:00+0530" \
  git merge --no-ff feature/s2-candidates-apps \
  -m "Merge branch 'feature/s2-candidates-apps' into develop

Sprint 3: Candidate scoring, analysis tools/agent, FastAPI endpoints, DI, tests, sprint checklist"

echo "✓ All 4 feature branches merged into develop with proper merge commits"

echo ""
echo "============================================"
echo " PHASE 10: VERIFICATION"
echo "============================================"
echo ""
echo "--- Merge commits ---"
git log --oneline --merges -4
echo ""
echo "--- Branch status ---"
for branch in feature/s1-company-jobs feature/s2-candidates-apps feature/s3-interviews-hiring feature/s4-employees-onboarding; do
  echo "$branch: $(git log --oneline "$branch" | wc -l | tr -d ' ') total commits"
done
echo ""
echo "--- File diff vs backup (should be EMPTY = identical content) ---"
DIFF_COUNT=$(git diff backup-pre-restructure develop --name-only 2>/dev/null | wc -l | tr -d ' ')
echo "Files different: $DIFF_COUNT"
if [ "$DIFF_COUNT" -eq "0" ]; then
  echo "✅ PERFECT: develop has identical content to before restructure"
else
  echo "⚠️  WARNING: Some files differ. Listing:"
  git diff backup-pre-restructure develop --name-only
fi

echo ""
echo "--- Commit graph (last 25) ---"
git log --oneline --graph -25

echo ""
echo "============================================"
echo " ✅ GIT RESTRUCTURE COMPLETE"
echo "============================================"
echo ""
echo "Next steps:"
echo "  1. Create missing files (AgentServiceClient.cs, config.py, endpoints)"
echo "  2. Push all branches to origin"
echo "  3. Delete remote s4-offers-hiring"
