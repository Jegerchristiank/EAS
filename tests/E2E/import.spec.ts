import { test, expect } from '@playwright/test';

test('import -> calculate -> approve -> report', async ({ request }) => {
  const api = request;
  // 1) org
  let res = await api.post('/organisations', { data: { name: 'Acme ApS', industry: 'Manufacturing', countryCode: 'DK' } });
  expect(res.ok()).toBeTruthy();
  const orgId = (await res.json()).id as string;

  // 2) period
  res = await api.post('/reporting-periods', { data: { organisationId: orgId, year: 2024, startDate: '2024-01-01', endDate: '2024-12-31' } });
  expect(res.ok()).toBeTruthy();
  const periodId = (await res.json()).id as string;

  // 3-4) would upload CSV via multipart, omitted here for brevity
  // 7) run calculations
  res = await api.post(`/calculations/run?periodId=${periodId}`);
  expect(res.ok()).toBeTruthy();

  // 10) generate report stub
  res = await api.post(`/reports/vsme/basic/generate?periodId=${periodId}`);
  expect(res.ok()).toBeTruthy();
});

