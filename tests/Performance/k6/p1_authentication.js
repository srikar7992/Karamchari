import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('error_rate');
const loginDuration = new Trend('login_duration');

export const options = {
  vus: 100,
  duration: '2m',
  thresholds: {
    'http_req_duration{name:login}': ['p(95)<500', 'p(99)<1000'],
    error_rate: ['rate<0.01'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Pre-generated test users seeded in the dev environment
const TEST_CREDENTIALS = [
  { email: 'load-user-01@acme.karamchari.com', password: 'LoadTest@123!' },
  { email: 'load-user-02@acme.karamchari.com', password: 'LoadTest@123!' },
  { email: 'load-user-03@acme.karamchari.com', password: 'LoadTest@123!' },
  { email: 'load-user-04@acme.karamchari.com', password: 'LoadTest@123!' },
  { email: 'load-user-05@acme.karamchari.com', password: 'LoadTest@123!' },
];

export default function () {
  const cred = TEST_CREDENTIALS[__VU % TEST_CREDENTIALS.length];

  const payload = JSON.stringify({
    email: cred.email,
    password: cred.password,
  });

  const params = {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'login' },
  };

  const res = http.post(`${BASE_URL}/api/identity/login`, payload, params);

  loginDuration.add(res.timings.duration);

  const success = check(res, {
    'login status is 200 or 401': (r) => r.status === 200 || r.status === 401,
    'login responded in < 1000ms': (r) => r.timings.duration < 1000,
  });

  // Count as error only on server-side failures (5xx), not auth failures (401)
  errorRate.add(res.status >= 500);

  sleep(0.5);
}
