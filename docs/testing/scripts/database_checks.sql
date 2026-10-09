\set ON_ERROR_STOP on
BEGIN;
DO $$
DECLARE sample "Applications"%ROWTYPE; n integer;
BEGIN
 SELECT * INTO STRICT sample FROM "Applications" ORDER BY "CreatedAt" DESC LIMIT 1;
 BEGIN
  INSERT INTO "Applications" ("Id","JobId","CandidateProfileId","Status","SubmittedAt","CreatedAt","UpdatedAt")
   VALUES (gen_random_uuid(),sample."JobId",sample."CandidateProfileId",'Submitted',now(),now(),now());
  RAISE EXCEPTION 'TC-DB-001 FAIL duplicate pair accepted';
 EXCEPTION WHEN unique_violation THEN RAISE NOTICE 'TC-DB-001 PASS duplicate job candidate pair rejected'; END;
 BEGIN
  INSERT INTO "ApplicationHistory" ("Id","ApplicationId","FromStatus","ToStatus","ChangedAt","CreatedAt","UpdatedAt")
   VALUES (gen_random_uuid(),gen_random_uuid(),'Submitted','Withdrawn',now(),now(),now());
  RAISE EXCEPTION 'TC-DB-002 FAIL orphan history accepted';
 EXCEPTION WHEN foreign_key_violation THEN RAISE NOTICE 'TC-DB-002 PASS orphan history rejected'; END;
 BEGIN
  UPDATE "Applications" SET "Status"=NULL WHERE "Id"=sample."Id";
  RAISE EXCEPTION 'TC-DB-003 FAIL null status accepted';
 EXCEPTION WHEN not_null_violation THEN RAISE NOTICE 'TC-DB-003 PASS null status rejected'; END;
 SELECT count(*) INTO n FROM "Applications" a JOIN "ApplicationHistory" h ON h."ApplicationId"=a."Id"
  WHERE a."Status"='Withdrawn' AND h."ToStatus"='Withdrawn';
 IF n < 1 THEN RAISE EXCEPTION 'TC-DB-004 FAIL no persisted withdrawal history'; END IF;
 RAISE NOTICE 'TC-DB-004 PASS live withdrawal and history persisted together';
END $$;
SAVEPOINT rollback_check;
UPDATE "Applications" SET "CoverLetter"='SE3110 rollback sentinel';
ROLLBACK TO SAVEPOINT rollback_check;
DO $$ BEGIN
 IF EXISTS (SELECT 1 FROM "Applications" WHERE "CoverLetter"='SE3110 rollback sentinel') THEN
  RAISE EXCEPTION 'TC-DB-005 FAIL transaction rollback did not restore rows'; END IF;
 RAISE NOTICE 'TC-DB-005 PASS rollback restores prior application data';
END $$;
ROLLBACK;
SELECT version();
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
