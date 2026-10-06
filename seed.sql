DO $$ 
DECLARE
    company_id uuid;
    dept_id uuid;
    profile_id uuid;
    new_job_id1 uuid := gen_random_uuid();
    new_job_id2 uuid := gen_random_uuid();
    new_job_id3 uuid := gen_random_uuid();
BEGIN
    SELECT "Id" INTO company_id FROM "Companies" LIMIT 1;
    SELECT "Id" INTO dept_id FROM "Departments" LIMIT 1;
    SELECT "Id" INTO profile_id FROM "CandidateProfiles" LIMIT 1;

    -- Create 3 new jobs
    INSERT INTO "Jobs" ("Id", "CompanyId", "DepartmentId", "Title", "Description", "EmploymentType", "Location", "MinimumExperience", "VacancyCount", "ApplicationDeadline", "Status", "CreatedAt", "UpdatedAt")
    VALUES 
    (new_job_id1, company_id, dept_id, 'Frontend Developer', 'Looking for React expert.', 'Full Time', 'Remote', 3, 1, now() + interval '30 days', 0, now(), now()),
    (new_job_id2, company_id, dept_id, 'Data Scientist', 'Looking for AI/ML expert.', 'Full Time', 'Remote', 4, 1, now() + interval '30 days', 0, now(), now()),
    (new_job_id3, company_id, dept_id, 'DevOps Engineer', 'Looking for AWS/Docker expert.', 'Full Time', 'Remote', 5, 1, now() + interval '30 days', 0, now(), now());

    -- Create applications for these jobs
    INSERT INTO "Applications" ("Id", "JobId", "CandidateProfileId", "Status", "SubmittedAt", "CoverLetter", "CreatedAt", "UpdatedAt")
    VALUES 
    (gen_random_uuid(), new_job_id1, profile_id, 0, now(), 'Generated test application for Frontend.', now(), now()),
    (gen_random_uuid(), new_job_id2, profile_id, 0, now(), 'Generated test application for Data Sci.', now(), now()),
    (gen_random_uuid(), new_job_id3, profile_id, 0, now(), 'Generated test application for DevOps.', now(), now());

    -- Reset the original application to Submitted
    UPDATE "Applications" SET "Status" = 0;
END $$;
