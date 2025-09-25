# Security

## Authentication & RBAC
- Cookie-based Identity with roles: Admin, SustainabilityLead, DataSteward, Auditor
- Policies enforced server-side on all write endpoints

## Data
- In transit: HTTPS enforced
- At rest: Use PostgreSQL with TDE/disk encryption in prod; SQLite for dev only

## Evidence Uploads
- Presigned upload token, MIME whitelist (pdf/png/jpeg), max 10MB
- Add AV scan hook (stub point in EvidenceController)

## Rate Limiting & CORS
- Global per-user/IP rate limit
- CORS restricted (dev: localhost; prod: configured origin)
