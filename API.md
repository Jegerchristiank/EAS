# API Overview

- Auth: Cookie-based Identity; RBAC policies: Admin, SustainabilityLead, DataSteward, Auditor
- Error shape: `{ code, field, message }` for 400 validation errors
- Paging: All list endpoints accept `page`, `pageSize` and return `{ total, page, pageSize, items }`

## Organisations
- GET `/organisations?q=&page=&pageSize=` -> 200
- POST `/organisations` (CanIngestData)
  ```json
  { "name":"Acme ApS", "industry":"Manufacturing", "countryCode":"DK", "organizationNumber":"12345678" }
  ```
- PUT `/organisations/{id}` (CanIngestData)

## Reporting Periods
- GET `/reporting-periods?organisationId=&page=&pageSize=` -> 200
- POST `/reporting-periods` (CanIngestData)
  ```json
  { "organisationId":"...", "year":2024, "startDate":"2024-01-01", "endDate":"2024-12-31" }
  ```

## Units
- GET `/units?page=&pageSize=`
- POST `/units` (CanManageReferenceData)
  ```json
  { "code":"kWh", "name":"Kilowatt-hour" }
  ```
- POST `/units/conversions` (CanManageReferenceData)
  ```json
  { "fromUnitId":"...", "toUnitId":"...", "factor":0.001 }
  ```

## Emission Factors
- GET `/emission-factors?country=DK&year=2024&type=electricity&page=&pageSize=`
- POST `/emission-factors` (CanManageReferenceData)

## Import CSV
- POST `/imports/csv/analyze` (multipart form: `file=@file.csv`)
- POST `/imports/csv/commit` (multipart form)
  - fields: `file`, `organisationId`, `periodId`, `mapping` (JSON string)
  - mapping must include `category`, `quantity`, `unitCode`, optional `date`, `scope`, `toUnitCode`. If `toUnitCode` given, conversion must exist.

## Evidence
- POST `/evidence/presign` (CanIngestData)
  ```json
  { "scopeEntryId":"...", "fileName":"invoice-123.pdf" }
  ```
- PUT `/evidence/upload/{token}` (Content-Type: pdf/png/jpeg, max 10MB)

## Activities / Scope Entries
- GET `/activities?periodId=&q=&page=&pageSize=`
- POST `/activities` (CanIngestData)
- GET `/scope-entries?activityId=&page=&pageSize=`
- POST `/scope-entries` (CanIngestData)

## Calculations
- POST `/calculations/run?periodId=...` (CanCalculate)
  - Requires at least one evidence or a deviation per ScopeEntry

## Approvals
- POST `/approvals/submit` (CanCalculate)
  ```json
  { "reportingPeriodId":"..." }
  ```
- PATCH `/approvals/{id}` (CanApprove)
  ```json
  { "status":"Approved|Rejected", "comment":"required" }
  ```

## Reports
- POST `/reports/vsme/basic/generate?periodId=...` -> `{ period, report }`
- POST `/reports/export/xbrl` (body: VsmeBasicReport) -> XML file
