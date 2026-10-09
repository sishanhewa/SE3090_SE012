# Git delivery status

Shared work is on develop under sishanhewa, with email sishanhewa4@gmail.com. Shared CI repair 3983a74 and application/React pull requests 2 and 3 are pushed and merged. The final report/evidence revision is also delivered on develop; the bundle's delivery-manifest.json records its exact SHA.

- https://github.com/sishanhewa/SE3090_SE012/pull/2
- https://github.com/sishanhewa/SE3090_SE012/pull/3

Only the lead account is authenticated. The priority instructions require each responsible member's account for component pushes. Other member pushes remain pending; they were not performed using the lead's credentials. Configured commit authors identify assigned ownership, not proof that a student personally authored or demonstrated AI-assisted work.

Prepared local component branches:
- feature/s1-company-jobs: vihara-rosa / rosavihara@gmail.com / 6cfc9bb32605dc1a6f8bff29cfef59feb6b43420
- feature/s2-candidates-apps: sishanhewa / sishanhewa4@gmail.com / b129a30231c0bfebbdea63dee70f796b430c51e1
- feature/s3-interviews-hiring: gaveeshamadhushan / gaveeshamadhushan15@gmail.com / fc8dd245c6d0275705afa4ca78edfaf728d760fc (test commit 755a7e8)
- feature/s4-employees-onboarding: chenu222 / dewminichethani222@gmail.com / 5340ee3e2a8d92220e19146fa6414cdf2f97e42c

The M4 final commit repairs employee UTC storage, duplicate onboarding route, and new status history insertion, with regression tests. It follows a merge of develop. All five branches are retained in testing-branches.bundle. The exact local verification composite is supplied separately because incomplete member pushes mean remote develop differs.

To deliver a pending member branch, authenticate its required account, verify identity, fetch the bundle into that member's checkout, merge latest origin/develop under the required configured author, rerun affected tests, push the prescribed branch and create its PR into develop. Never replace the responsible member's authentication with another account without an explicit change to the priority instruction.

Native GitHub screenshots show backend, React and Flutter passes at their recorded source SHAs and an AI failure before the local member fix. They do not assert that the complete 205-case local composite already passed remote CI.
