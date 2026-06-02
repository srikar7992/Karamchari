import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Per-step latency metrics
const stepRequisition = new Trend('step_create_requisition_ms');
const stepCandidate = new Trend('step_create_candidate_ms');
const stepApplication = new Trend('step_apply_candidate_ms');
const stepAdvance = new Trend('step_advance_application_ms');
const stepInterview = new Trend('step_schedule_interview_ms');
const stepOffer = new Trend('step_create_offer_ms');
const stepApprove = new Trend('step_approve_offer_ms');
const stepIssue = new Trend('step_issue_offer_ms');
const stepAccept = new Trend('step_accept_offer_ms');
const stepHire = new Trend('step_hire_candidate_ms');
const journeyDuration = new Trend('journey_total_ms');
const errorRate = new Rate('error_rate');

export const options = {
  vus: 100,
  duration: '5m',
  thresholds: {
    journey_total_ms: ['p(95)<10000'],
    step_create_requisition_ms: ['p(95)<500'],
    step_create_candidate_ms: ['p(95)<500'],
    step_apply_candidate_ms: ['p(95)<500'],
    step_hire_candidate_ms: ['p(95)<1000'],
    error_rate: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TENANT_HOST = __ENV.TENANT_HOST || 'acme.karamchari.com';

// Shared auth token obtained during setup
let authToken = '';

export function setup() {
  // Login as a recruiter to get a token
  const res = http.post(
    `${BASE_URL}/api/identity/login`,
    JSON.stringify({ email: 'recruiter@acme.karamchari.com', password: 'Recruiter@123!' }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (res.status !== 200) {
    // Return empty token; VUs will use it and get 401 which counts as an error
    console.warn(`Setup login failed: ${res.status} ${res.body}`);
    return { token: '' };
  }

  const body = JSON.parse(res.body);
  return { token: body.accessToken || body.token || '' };
}

function authHeaders(token) {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
    Host: TENANT_HOST,
  };
}

function postJson(url, body, token, tag) {
  const start = Date.now();
  const res = http.post(url, JSON.stringify(body), {
    headers: authHeaders(token),
    tags: { name: tag },
  });
  return { res, elapsed: Date.now() - start };
}

export default function (data) {
  const token = data.token;
  const journeyStart = Date.now();

  // Step 1: Create requisition
  const deptId = '00000000-0000-0000-0000-000000000001';
  const managerId = '00000000-0000-0000-0000-000000000002';
  const { res: reqRes, elapsed: reqMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/requisitions`,
    { title: `Load Test Role ${__VU}-${__ITER}`, departmentId: deptId, hiringManagerId: managerId },
    token,
    'create_requisition'
  );
  stepRequisition.add(reqMs);
  errorRate.add(reqRes.status >= 500);
  if (!check(reqRes, { 'requisition created (201)': (r) => r.status === 201 })) {
    return; // Cannot continue without requisition
  }
  const requisitionId = JSON.parse(reqRes.body).id;

  // Step 2: Create candidate
  const { res: candRes, elapsed: candMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/candidates`,
    {
      firstName: `Candidate${__VU}`,
      lastName: `Iter${__ITER}`,
      email: `cand-${__VU}-${__ITER}@loadtest.example.com`,
      phoneNumber: null,
    },
    token,
    'create_candidate'
  );
  stepCandidate.add(candMs);
  errorRate.add(candRes.status >= 500);
  if (!check(candRes, { 'candidate created (201)': (r) => r.status === 201 })) {
    return;
  }
  const candidateId = JSON.parse(candRes.body).id;

  // Step 3: Apply candidate to requisition
  const { res: appRes, elapsed: appMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/applications`,
    { candidateId, requisitionId },
    token,
    'apply_candidate'
  );
  stepApplication.add(appMs);
  errorRate.add(appRes.status >= 500);
  if (!check(appRes, { 'application created (201)': (r) => r.status === 201 })) {
    return;
  }
  const applicationId = JSON.parse(appRes.body).id;

  // Step 4: Advance application
  const { res: advRes, elapsed: advMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/applications/${applicationId}/advance`,
    {},
    token,
    'advance_application'
  );
  stepAdvance.add(advMs);
  errorRate.add(advRes.status >= 500);

  // Step 5: Schedule interview
  const interviewAt = new Date(Date.now() + 86400000).toISOString();
  const { res: intRes, elapsed: intMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/interviews`,
    { applicationId, scheduledAt: interviewAt, durationMinutes: 60, interviewerIds: [managerId] },
    token,
    'schedule_interview'
  );
  stepInterview.add(intMs);
  errorRate.add(intRes.status >= 500);

  // Step 6: Create offer
  const { res: offerRes, elapsed: offerMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/offers`,
    { applicationId, baseSalary: 80000, currency: 'INR' },
    token,
    'create_offer'
  );
  stepOffer.add(offerMs);
  errorRate.add(offerRes.status >= 500);
  if (!check(offerRes, { 'offer created (201)': (r) => r.status === 201 })) {
    return;
  }
  const offerId = JSON.parse(offerRes.body).id;

  // Step 7: Approve offer
  const { res: approveRes, elapsed: approveMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/offers/${offerId}/approve`,
    {},
    token,
    'approve_offer'
  );
  stepApprove.add(approveMs);
  errorRate.add(approveRes.status >= 500);

  // Step 8: Issue offer
  const expiresAt = new Date(Date.now() + 7 * 86400000).toISOString();
  const { res: issueRes, elapsed: issueMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/offers/${offerId}/issue`,
    { expiresAt },
    token,
    'issue_offer'
  );
  stepIssue.add(issueMs);
  errorRate.add(issueRes.status >= 500);

  // Step 9: Accept offer
  const { res: acceptRes, elapsed: acceptMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/offers/${offerId}/accept`,
    {},
    token,
    'accept_offer'
  );
  stepAccept.add(acceptMs);
  errorRate.add(acceptRes.status >= 500);

  // Step 10: Hire candidate
  const { res: hireRes, elapsed: hireMs } = postJson(
    `${BASE_URL}/api/v1/recruitment/applications/${applicationId}/hire`,
    {},
    token,
    'hire_candidate'
  );
  stepHire.add(hireMs);
  errorRate.add(hireRes.status >= 500);
  check(hireRes, { 'hire succeeded (204)': (r) => r.status === 204 });

  journeyDuration.add(Date.now() - journeyStart);

  sleep(1);
}
