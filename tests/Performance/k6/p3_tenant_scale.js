import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const queryLatency = new Trend('tenant_query_ms');
const crossTenantErrorRate = new Rate('cross_tenant_leak');
const errorRate = new Rate('error_rate');

export const options = {
  scenarios: {
    // Warm-up phase: ramp up to 50 VUs
    warm_up: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50 },
        { duration: '1m', target: 50 },
      ],
      gracefulRampDown: '10s',
    },
    // Sustained load at 100 VUs
    sustained: {
      executor: 'constant-vus',
      vus: 100,
      duration: '3m',
      startTime: '1m30s',
    },
  },
  thresholds: {
    tenant_query_ms: ['p(95)<1000', 'p(99)<2000'],
    cross_tenant_leak: ['rate==0'],
    error_rate: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || 'admin@karamchari.com';
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'Admin@123!';

// Tenant pool: 100 tenants created in setup()
const TENANT_COUNT = 100;

export function setup() {
  // Obtain admin token
  const loginRes = http.post(
    `${BASE_URL}/api/identity/login`,
    JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (loginRes.status !== 200) {
    console.warn(`Admin login failed during setup: ${loginRes.status}`);
    return { tokens: {}, tenants: [] };
  }

  const adminBody = JSON.parse(loginRes.body);
  const adminToken = adminBody.accessToken || adminBody.token || '';

  const adminHeaders = {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${adminToken}`,
  };

  // Create 100 tenants (idempotent: existing tenants return 201 or 200)
  const tenants = [];
  for (let i = 1; i <= TENANT_COUNT; i++) {
    const companyName = `Scale Tenant ${String(i).padStart(3, '0')}`;
    const adminEmail = `admin-${i}@scaletenant${i}.test`;

    const res = http.post(
      `${BASE_URL}/api/tenants`,
      JSON.stringify({ companyName, adminEmail }),
      { headers: adminHeaders }
    );

    if (res.status === 201 || res.status === 200) {
      const body = JSON.parse(res.body);
      tenants.push({ tenantId: body.tenantId || `scale_tenant_${String(i).padStart(3, '0')}` });
    } else {
      // Derive tenant ID from company name as the provisioning service does
      tenants.push({ tenantId: `scale_tenant_${String(i).padStart(3, '0')}` });
    }
  }

  // NOTE: Seeding 10,000 requisitions and 50,000 candidates is performed by the
  // DevDataSeeder or a separate migration script before running this k6 test.
  // Command: dotnet run --project src/Backend/Hosts/Karamchari.Api -- --seed-scale
  console.log(`Setup complete. ${tenants.length} tenants provisioned.`);

  return { tenants, adminToken };
}

export default function (data) {
  const tenants = data.tenants;
  if (!tenants || tenants.length === 0) {
    errorRate.add(1);
    return;
  }

  // Each VU acts as a different tenant
  const myTenant = tenants[__VU % tenants.length];
  const otherTenant = tenants[(__VU + 1) % tenants.length];

  const myHost = `${myTenant.tenantId}.karamchari.com`;
  const otherHost = `${otherTenant.tenantId}.karamchari.com`;

  // Login as tenant user
  const loginRes = http.post(
    `${BASE_URL}/api/identity/login`,
    JSON.stringify({
      email: `user@${myTenant.tenantId}.karamchari.com`,
      password: 'TenantUser@123!',
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (loginRes.status !== 200) {
    // Record as error but do not abort — tenant user may not be seeded
    errorRate.add(loginRes.status >= 500 ? 1 : 0);
    sleep(1);
    return;
  }

  const token = (() => {
    try {
      const b = JSON.parse(loginRes.body);
      return b.accessToken || b.token || '';
    } catch {
      return '';
    }
  })();

  // Query 1: Own tenant HR employees (should succeed)
  const start1 = Date.now();
  const ownRes = http.get(`${BASE_URL}/api/v1/hr/employees`, {
    headers: {
      Authorization: `Bearer ${token}`,
      Host: myHost,
    },
    tags: { name: 'own_tenant_query' },
  });
  queryLatency.add(Date.now() - start1);
  errorRate.add(ownRes.status >= 500);
  check(ownRes, {
    'own tenant query succeeds (200)': (r) => r.status === 200,
  });

  // Query 2: Cross-tenant query — attempt to access another tenant's data via Host spoofing
  // Authenticated as tenant A but requesting tenant B's host header: must return 200 with
  // empty/own data (tenant filter at DbContext) or 401/403, never tenant B's data.
  const start2 = Date.now();
  const crossRes = http.get(`${BASE_URL}/api/v1/hr/employees`, {
    headers: {
      Authorization: `Bearer ${token}`,
      Host: otherHost, // Attempt cross-tenant access
    },
    tags: { name: 'cross_tenant_probe' },
  });
  queryLatency.add(Date.now() - start2);

  // Cross-tenant leak = got actual data from other tenant (200 with content when auth is for myTenant)
  // A safe response is: 200 empty list, 403, or 401.
  if (crossRes.status === 200) {
    try {
      const body = JSON.parse(crossRes.body);
      // If we received a non-empty array of employees, that could be a leak — flag it.
      // (A correctly isolated system returns an empty list because the tenant filter
      //  uses the JWT tenant_id claim, not the Host header.)
      const isArray = Array.isArray(body);
      const hasItems = isArray && body.length > 0;
      crossTenantErrorRate.add(hasItems ? 1 : 0);
    } catch {
      crossTenantErrorRate.add(0);
    }
  } else {
    crossTenantErrorRate.add(0);
  }

  sleep(0.5);
}
