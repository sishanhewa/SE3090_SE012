"""Live interview safe-failure and independent employee onboarding checks.
Run only against the disposable verification database. No external invitations.
"""
from pathlib import Path
from datetime import datetime, timedelta, timezone
source=Path(__file__).with_name('live_api_tests.py').read_text()
exec(compile(source.split('\ntry:\n')[0], str(Path(__file__).with_name('live_api_tests.py')), 'exec'))
ids={}
def c(n,name,method,path,expected,data=None,token=None,predicate=None):
 return check(f'TC-ONLIVE-{n:03}',name,method,path,expected,data,token,predicate)
future=lambda d:(datetime.now(timezone.utc)+timedelta(days=d)).isoformat()
try:
 admin=c(1,'Administrator login','POST','/Auth/login',200,{'email':os.getenv('TEST_ADMIN_EMAIL','admin@talentflow.com'),'password':os.getenv('TEST_ADMIN_PASSWORD','Admin@123456')},predicate=lambda b:bool(b.get('accessToken')));at=admin['accessToken']
 user=c(2,'Register synthetic onboarding candidate','POST','/Auth/register',201,{'email':f'onboarding-{uuid.uuid4().hex[:10]}@example.test','password':'Evidence@123456','firstName':'SE3110','lastName':'Onboarding Evidence','role':'Candidate'},predicate=lambda b:bool(b.get('accessToken')));ct=user['accessToken'];uid=user['user']['id']
 c(3,'Create onboarding candidate profile','POST','/CandidateProfiles',201,{'summary':'Synthetic SE3110 verification profile'},ct)
 jobs=c(4,'Retrieve published job fixture','GET','/jobs',200,predicate=lambda b:any(j['status']=='Published' for j in b['items']));job=next(j for j in jobs['items'] if j['status']=='Published');cid=job['companyId'];did=job['departmentId']
 app=c(5,'Submit interview candidate application','POST',f'/Applications/jobs/{job["id"]}',201,{'coverLetter':'Synthetic interview safe-failure verification'},ct,predicate=lambda b:b['status']=='Submitted');aid=app['id']
 c(6,'Move application into screening','PATCH',f'/Applications/{aid}/status',204,{'status':'Screening','notes':'Verification review'},at)
 c(7,'Direct shortlist cannot bypass reviewed AI workflow','PATCH',f'/Applications/{aid}/status',400,{'status':'Shortlisted','notes':'Verification shortlist'},at,predicate=lambda b:'Invalid status transition' in str(b))
 c(8,'Unshortlisted application cannot schedule interview','POST','/Interviews',400,{'applicationId':aid,'scheduledAt':future(3),'durationMinutes':60},at,predicate=lambda b:'Shortlisted' in str(b))
 c(9,'Unknown interview does not disclose data','GET','/Interviews/00000000-0000-0000-0000-000000000001',403,token=at)
 c(10,'Candidate cannot create an interview','POST','/Interviews',403,{'applicationId':aid,'scheduledAt':future(3),'durationMinutes':60},ct)
 c(11,'Offer requires completed interview','POST','/Offers',400,{'applicationId':aid,'position':'QA Evidence Engineer','salary':150000,'startDate':future(30),'expiryDate':future(14)},at,predicate=lambda b:'interview' in str(b).lower())
 employee=c(12,'Staff creates separate onboarding employee','POST',f'/companies/{cid}/employees',201,{'userId':uid,'departmentId':did,'employeeNumber':'SE3110-'+uuid.uuid4().hex[:8],'position':'QA Evidence Engineer','startDate':future(30)},at,predicate=lambda b:b['status']=='Onboarding');eid=employee['id']
 c(13,'Activation requires assigned template','PUT',f'/companies/{cid}/employees/{eid}',400,{'status':'Active'},at,predicate=lambda b:'template' in str(b).lower())
 c(14,'Empty onboarding template rejected','POST',f'/companies/{cid}/onboarding/templates',400,{'name':'Invalid evidence template','tasks':[]},at)
 template=c(15,'Create two-task onboarding template','POST',f'/companies/{cid}/onboarding/templates',200,{'name':'SE3110 Verification Induction '+uuid.uuid4().hex[:6],'description':'Synthetic report verification','tasks':[{'title':'Review induction guide','sortOrder':1,'isMandatory':True},{'title':'Confirm workspace access','sortOrder':2,'isMandatory':True}]},at,predicate=lambda b:len(b['tasks'])==2);tid=template['id']
 tasks=c(16,'Assign onboarding tasks once','POST',f'/employees/{eid}/onboarding/assign/{tid}',200,token=at,predicate=lambda b:len(b)==2 and not any(x['isCompleted'] for x in b))
 c(17,'Duplicate assignment rejected','POST',f'/employees/{eid}/onboarding/assign/{tid}',400,token=at,predicate=lambda b:'already' in str(b).lower())
 c(18,'Activation blocked by mandatory tasks','PUT',f'/companies/{cid}/employees/{eid}',400,{'status':'Active'},at,predicate=lambda b:'required' in str(b).lower())
 c(19,'Owner can read assigned tasks','GET',f'/employees/{eid}/onboarding',200,token=ct,predicate=lambda b:len(b)==2)
 c(20,'Anonymous task access rejected','GET',f'/employees/{eid}/onboarding',401)
 c(21,'Complete first task as owner','PATCH',f'/onboarding/tasks/{tasks[0]["id"]}',200,{'notes':'Induction guide reviewed in verification'},ct,predicate=lambda b:b['isCompleted'] and bool(b['completedAt']))
 c(22,'One incomplete task still blocks activation','PUT',f'/companies/{cid}/employees/{eid}',400,{'status':'Active'},at)
 c(23,'Complete second task as owner','PATCH',f'/onboarding/tasks/{tasks[1]["id"]}',200,{'notes':'Workspace access confirmed in verification'},ct,predicate=lambda b:b['isCompleted'])
 c(24,'Staff activates employee after completion','PUT',f'/companies/{cid}/employees/{eid}',200,{'status':'Active'},at,predicate=lambda b:b['status']=='Active')
 c(25,'Persisted employee remains active','GET',f'/companies/{cid}/employees/{eid}',200,token=at,predicate=lambda b:b['status']=='Active' and b['id']==eid)
 c(26,'Completed tasks persist in order','GET',f'/employees/{eid}/onboarding',200,token=at,predicate=lambda b:len(b)==2 and all(x['isCompleted'] for x in b) and b[0]['taskTitle']=='Review induction guide')
 ids=dict(applicationId=aid,employeeId=eid,companyId=cid,templateId=tid,candidateUserId=uid)
finally:
 output=Path(args.output);output.parent.mkdir(parents=True,exist_ok=True)
 output.write_text(json.dumps(dict(base_url=args.base_url,run_date='2026-10-09',checks=records,ids=ids,passed=sum(x['status']=='Pass' for x in records),failed=sum(x['status']=='Fail' for x in records),scope='Live API and PostgreSQL. Reviewed AI screening is not bypassed. Employee created independently through staff endpoint; no interview fixtures or data altered to bypass approval.'),indent=2))
