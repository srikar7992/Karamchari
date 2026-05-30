import json
import urllib.request
import urllib.parse
import os
import subprocess
import time

base_url = 'http://localhost:60463'
csv_path = 'sample-data/migration/v1/employees.csv'

def request(url, method='GET', data=None, headers=None, multipart=False):
    if headers is None: headers = {}
    if data and not multipart:
        data = json.dumps(data).encode('utf-8')
        headers['Content-Type'] = 'application/json'
    
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req) as res:
            return res.getcode(), json.loads(res.read().decode())
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()
    except Exception as e:
        return 0, str(e)

# 1. Login
print("Step 1: Login...")
code, body = request(f'{base_url}/api/identity/login', 'POST', {'email':'admin@dev.local','password':'Dev@Pass123!','tenantId':'dev'})
if code != 200:
    print(f"Login failed: {code} {body}")
    exit(1)
token = body['accessToken']
headers = {'Authorization': f'Bearer {token}'}

# 2. Upload
print("Step 2: Uploading CSV...")
upload_cmd = [
    'curl', '-s', '-X', 'POST', f'{base_url}/api/v1/migration/imports',
    '-H', f'Authorization: Bearer {token}',
    '-F', 'importType=EmployeeImport',
    '-F', 'file=@' + csv_path
]
result = subprocess.run(upload_cmd, capture_output=True, text=True)
if result.returncode != 0 or 'id' not in result.stdout:
    print(f"Upload failed: {result.stdout}")
    exit(1)
upload_body = json.loads(result.stdout)
job_id = upload_body['id']
print(f"Job created: {job_id}")

# 3. Preview
print("Step 3: Getting Preview...")
code, body = request(f'{base_url}/api/v1/migration/imports/{job_id}/preview', 'GET', headers=headers)
print(f"Preview Status: {code}")
if code == 200:
    print(f"Preview Rows: {body['totalRowsPreviewed']}")

# 4. Validate
print("Step 4: Validating Import...")
code, body = request(f'{base_url}/api/v1/migration/imports/{job_id}/validate', 'POST', headers=headers)
print(f"Validation Status: {code}")
if code == 200:
    print(f"Valid Rows: {body['validRows']}, Invalid: {body['invalidRows']}")

# 5. Execute
print("Step 5: Executing Import...")
code, body = request(f'{base_url}/api/v1/migration/imports/{job_id}/execute', 'POST', headers=headers)
print(f"Execution Status: {code}")

# 6. Monitor
print("Step 6: Monitoring Status...")
for _ in range(10):
    time.sleep(2)
    code, body = request(f'{base_url}/api/v1/migration/imports/{job_id}', 'GET', headers=headers)
    print(f"Current Status: {body['status']}")
    if body['status'] in [6, 7, 8, 9]: # Completed, CompletedWithErrors, Failed, Cancelled
        break

# 7. Final Verification
print("Step 7: Verifying HR Data...")
code, body = request(f'{base_url}/api/v1/hr/employees', 'GET', headers=headers)
if code == 200:
    count = sum(1 for e in body if e['employeeNumber'].startswith('EMP-M-'))
    print(f"Verified {count} new employees in HR module.")
