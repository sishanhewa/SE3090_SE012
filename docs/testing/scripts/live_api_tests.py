"""Reproducible API integration/security checks against an isolated TalentFlow DB.
Run: python docs/testing/scripts/live_api_tests.py --base-url http://127.0.0.1:5155/api
Uses seeded development admin; never target a production database.
"""
import argparse, json, os, time, uuid
from urllib.request import Request, urlopen
from urllib.error import HTTPError
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--base-url', default='http://127.0.0.1:5155/api')
parser.add_argument('--output', default='docs/testing/evidence/live-api.json')
args = parser.parse_args()
records = []
def request(method, path, data=None, token=None):
    headers = {'Content-Type':'application/json'}
    if token: headers['Authorization'] = 'Bearer ' + token
    req = Request(args.base_url+path, data=json.dumps(data).encode() if data is not None else None, headers=headers, method=method)
    try:
        with urlopen(req, timeout=30) as response: status, body = response.status, response.read().decode()
    except HTTPError as e: status, body = e.code, e.read().decode()
    try: body = json.loads(body)
    except ValueError: pass
    return status, body

def check(id, name, method, path, expected, data=None, token=None, predicate=None, detail=''):
    start=time.perf_counter(); status,body=request(method,path,data,token)
    ok=status in ([expected] if isinstance(expected,int) else expected)
    if predicate and ok: ok=bool(predicate(body))
    records.append(dict(id=id,name=name,method=method,path=path,input=data if not path.startswith('/Auth') else 'credentials redacted',expected=str(expected)+(' and body assertions' if predicate else ''),actual=f'HTTP {status}',status='Pass' if ok else 'Fail',duration_ms=round((time.perf_counter()-start)*1000,2),detail=detail))
    print(f'{id} {"PASS" if ok else "FAIL"} {name}: HTTP {status}',flush=True)
    if not ok: raise AssertionError(f'{name}: {status} {body}')
    return body

try:
    admin=check('TC-LIVE-001','Seeded admin authenticates','POST','/Auth/login',200,{'email':os.getenv('TEST_ADMIN_EMAIL','admin@talentflow.com'),'password':os.getenv('TEST_ADMIN_PASSWORD','Admin@123456')},predicate=lambda b: bool(b.get('accessToken')))
    at=admin['accessToken']
    check('TC-SEC-001','Protected companies reject anonymous access','GET','/Companies',401)
    check('TC-SEC-002','Malformed JWT is rejected','GET','/Auth/me',401,token='invalid.jwt.value')
    check('TC-SEC-003','Wrong password is rejected','POST','/Auth/login',401,{'email':'admin@talentflow.com','password':'incorrect-password'})
    check('TC-SEC-004','Public staff-role escalation is rejected','POST','/Auth/register',400,{'email':f'escalation-{uuid.uuid4().hex[:8]}@example.test','password':'Evidence@123456','firstName':'Test','lastName':'Escalation','role':'SystemAdmin'})
    check('TC-SEC-005','Malformed login input is rejected','POST','/Auth/login',400,{'email':'not-an-email','password':''})
    users=[]
    for i in range(2):
        email=f'se3110-{uuid.uuid4().hex[:10]}@example.test'
        u=check(f'TC-LIVE-00{2+i*2}',f'Register isolated candidate {i+1}','POST','/Auth/register',201,{'email':email,'password':'Evidence@123456','firstName':'Evidence','lastName':f'Candidate{i+1}','role':'Candidate'},predicate=lambda b: 'accessToken' in b)
        profile=check(f'TC-LIVE-00{3+i*2}',f'Create candidate profile {i+1}','POST','/CandidateProfiles',201,{'summary':'SE3110 isolated integration verification'},u['accessToken'],predicate=lambda b: bool(b.get('id')))
        users.append(u['accessToken'])
    ct,other=users
    check('TC-SEC-006','Candidate cannot create company','POST','/Companies',403,{'name':'Unauthorized evidence company'},ct)
    jobs=check('TC-LIVE-006','Public jobs can be browsed','GET','/jobs',200,predicate=lambda b: len(b['items'])>0)
    published=next(j for j in jobs['items'] if j['status']=='Published')
    job=published['id']
    app=check('TC-LIVE-007','Candidate submits to published job','POST',f'/Applications/jobs/{job}',201,{'coverLetter':'SE3110 live workflow evidence'},ct,predicate=lambda b: b['status']=='Submitted')
    aid=app['id']
    check('TC-LIVE-008','Candidate sees persisted application','GET',f'/Applications/{aid}',200,token=ct,predicate=lambda b:b['id']==aid and b['aiScore'] is None and b['aiRecommendation'] is None)
    check('TC-SEC-007','Other candidate cannot read application','GET',f'/Applications/{aid}',403,token=other)
    check('TC-SEC-008','Candidate cannot list staff applications','GET','/Applications',403,token=ct)
    check('TC-LIVE-009','Duplicate application is rejected','POST',f'/Applications/jobs/{job}',409,{'coverLetter':'Duplicate'},ct)
    check('TC-LIVE-010','Staff sees submitted application','GET',f'/Applications/{aid}',200,token=at,predicate=lambda b:b['id']==aid and b['status']=='Submitted')
    check('TC-LIVE-011','Candidate withdraws own application','POST',f'/Applications/{aid}/withdraw',204,token=ct)
    check('TC-LIVE-012','Staff sees persisted withdrawal','GET',f'/Applications/{aid}',200,token=at,predicate=lambda b:b['status']=='Withdrawn')
    check('TC-LIVE-013','Withdrawal is in application history','GET',f'/Applications/{aid}/history',200,token=at,predicate=lambda b:any(x['toStatus']=='Withdrawn' for x in b))
    check('TC-SEC-009','Other candidate cannot withdraw application','POST',f'/Applications/{aid}/withdraw',403,token=other)
finally:
    output=Path(args.output); output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps({'base_url':args.base_url,'run_date':'2026-10-09','checks':records,'passed':sum(x['status']=='Pass' for x in records),'failed':sum(x['status']=='Fail' for x in records)},indent=2))
