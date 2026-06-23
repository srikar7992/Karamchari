$base = "http://localhost:60463"
$token = Get-Content "C:\Users\srika\AppData\Local\Temp\opencode\jwt.txt" -Raw
$h = @{ "Authorization" = "Bearer $token"; "X-Tenant-Id" = "dev"; "X-Karamchari-Gateway" = "local-dev-gateway" }

function Post($path, $body) {
    $r = Invoke-WebRequest -UseBasicParsing -Uri "$base$path" -Method POST -Headers $h -ContentType "application/json" -Body $body -TimeoutSec 15
    Write-Host ("POST $path -> " + $r.StatusCode)
    return $r.Content
}

$reqBody = '{"title":"Smoke Engineer","departmentId":"11111111-1111-1111-1111-111111111111","hiringManagerId":"22222222-2222-2222-2222-222222222222"}'
$reqId = (Post "/api/v1/recruitment/requisitions" $reqBody | ConvertFrom-Json).id
Write-Host "reqId=$reqId"
Post "/api/v1/recruitment/requisitions/$reqId/publish" "{}" | Out-Null

$candBody = '{"firstName":"Smoke","lastName":"Candidate","email":"smoke@example.com","phoneNumber":"555-0000"}'
$candId = (Post "/api/v1/recruitment/candidates" $candBody | ConvertFrom-Json).id
Write-Host "candId=$candId"

$appBody = '{"candidateId":"' + $candId + '","requisitionId":"' + $reqId + '"}'
$appId = (Post "/api/v1/recruitment/applications" $appBody | ConvertFrom-Json).id
Write-Host "appId=$appId"

Post "/api/v1/recruitment/applications/$appId/advance" "{}" | Out-Null

$ivBody = '{"applicationId":"' + $appId + '","scheduledAt":"' + ([DateTimeOffset]::UtcNow.AddDays(1).ToString('o')) + '","durationMinutes":60,"interviewerIds":["33333333-3333-3333-3333-333333333333"]}'
$ivId = (Post "/api/v1/recruitment/interviews" $ivBody | ConvertFrom-Json).id
Write-Host "ivId=$ivId"

$fbBody = '{"interviewerId":"33333333-3333-3333-3333-333333333333","rating":5,"comments":"Smoke hire"}'
Post "/api/v1/recruitment/interviews/$ivId/feedback" $fbBody | Out-Null

$ofBody = '{"applicationId":"' + $appId + '","baseSalary":150000,"currency":"USD"}'
$ofId = (Post "/api/v1/recruitment/offers" $ofBody | ConvertFrom-Json).id
Write-Host "ofId=$ofId"

Post "/api/v1/recruitment/offers/$ofId/approve" "{}" | Out-Null
$isBody = '{"expiresAt":"' + ([DateTimeOffset]::UtcNow.AddDays(7).ToString('o')) + '"}'
Post "/api/v1/recruitment/offers/$ofId/issue" $isBody | Out-Null
Post "/api/v1/recruitment/offers/$ofId/accept" "{}" | Out-Null
Post "/api/v1/recruitment/applications/$appId/hire" "{}" | Out-Null

Write-Host "SMOKE_WRITE_DONE"
$appId | Out-File "C:\Users\srika\AppData\Local\Temp\opencode\smoke.appId.txt" -Encoding ascii -NoNewline
$candId | Out-File "C:\Users\srika\AppData\Local\Temp\opencode\smoke.candId.txt" -Encoding ascii -NoNewline
$ofId | Out-File "C:\Users\srika\AppData\Local\Temp\opencode\smoke.ofId.txt" -Encoding ascii -NoNewline