E2E Test Plan: Import → Calculate → Approve → Report

Preconditions
- API running (EsgAsAService.Api) with a test user having role DataSteward and Auditor.
- Database seeded with Units (kWh) and an EmissionFactorV2 for DK/2024 electricity (0.2 kg/kWh).

Flow
1) Create Organisation
   - POST /organisations
   - Expect 201 and Id
2) Create Reporting Period
   - POST /reporting-periods
3) Analyze CSV
   - POST /imports/csv/analyze with CSV (date,category,quantity,unitCode)
4) Commit CSV
   - POST /imports/csv/commit with mapping form field
   - Expect imported count > 0
5) Attach Evidence for each scope entry
   - POST /evidence/presign → PUT upload
6) Set Emission Factor on scope entries (if not set during import)
   - PUT /scope-entries/{id}
7) Run Calculations
   - POST /calculations/run?periodId=...
   - Expect results count equals scope entries
8) Submit Approval
   - POST /approvals/submit
9) Approve
   - PATCH /approvals/{id} with status=Approved
10) Generate VSME Basic
   - POST /reports/vsme/basic/generate?periodId=...
11) Export PDF + XBRL
   - POST /reports/export/pdf and /reports/export/xbrl

Assertions
- Every mutation returns 200/201 and well-formed payload; errors return {code, field, message} when invalid.
- Audit log has entries for inserts.

