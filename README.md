## Charter Reporter App – Technical Implementation Guide

### Purpose and Scope
- Deliver a secure, server-side-first ASP.NET Core MVC reporting app for Charter Academy admins (Charter, REBOSA, PPRA) to analyze Moodle and WooCommerce data with an emphasis on CPD compliance.
- Align with the Project Requirements Document and enforced rules in `.cursor`, including MASTER-CURSORRULES, Context, State, Templates, and Workflows.
- Out of scope: AI insights, offline access, data editing, integrations beyond the two source databases.

### Success Criteria
- Accurate cross-source joins with robust identity reconciliation.
- CPD per-user/per-year compliance visibility, including a fourth-completion spotlight.
- Clean RBAC with policy-based authorization and export safety.
- Responsive and accessible UI (WCAG 2.1), mobile-friendly, server-rendered with no client aggregation.

## Architecture Overview
- ASP.NET Core MVC with Identity for authentication/authorization.
- Clean layered structure: Controllers → Services → Repositories → DbContexts; ViewModels/DTOs only to Views; no Entities in Views.
- Datastores:
  - SQLite for app data (Identity, approvals, audit, exports, config).
  - MariaDB read-only connections for Moodle and WooCommerce.
- Data access:
  - EF Core retained for `AppDbContext` (SQLite, Identity, logs).
  - Dapper for MariaDB reporting queries (projections, aggregates, controlled hydration).
- Server-side-first enforcement: all aggregations, filtering, sorting, pagination on the server. Live data only.

## Project Structure
- Simple MVC modules aligned with Clean Architecture:
  - Web: Controllers and Views; server-rendered UI using SB Admin 2 components; no client-side aggregation.
  - Application: Use-case services (Dashboard, Compliance, Export, Approval, Filter Validation).
  - Domain: DTOs, value objects, policy constants; no EF entities exposed to Views.
  - Infrastructure: EF Core `AppDbContext` (SQLite), Dapper-based query modules for Moodle and WooCommerce, repositories.
  - Shared: Cross-cutting concerns (structured logging, correlation IDs, configuration, constants).
- Enforced separation: Controllers → Services → Repositories → DbContexts; Views consume ViewModels/DTOs only.

## Data Sources and Connectivity
- Two MariaDB read-only connections: Moodle and WooCommerce; least-privilege DB users with SELECT only.
- EF Core migrations for SQLite only; no migrations for source DBs.
- Mandatory filter bounds for all cross-DB reports (date range and category) to constrain query sizes and in-memory joins.
- Query budget guard: refuse execution if estimated rows exceed thresholds without further narrowing.

## RBAC and Policies
- Roles: Charter Admin, REBOSA Admin, PPRA Admin (centralized constants).
- Policy-based authorization per controller/action; deny by default.
- Column-level export authorization and redaction enforced by an Export Safety Router.

## Registration and Onboarding Data
- Required fields at registration:
  - Name (first, last)
  - Email
  - Organization
  - Role (requested)
  - ID number
  - Cell
  - Address
- Process:
  - Email verification is mandatory prior to access.
  - After verification, non-Charter roles enter an Approvals queue; only Charter Admin approves/rejects with reason and notification.
  - Inputs are validated server-side (FluentValidation); minimize PII in views/exports per role constraints.
- Compliance:
  - POPI-aligned handling throughout; do not log PII or secrets; apply export redaction by role.

## Security, Privacy, and Compliance
- Authentication: Identity with email verification; approval workflow for onboarding; password and session policies.
- Authorization: Role and policy checks across all entry points (controllers, exports).
- CSRF: Anti-forgery on all state-changing actions; tokens included in AJAX flows.
- XSS/CSP:
  - Strict Content Security Policy with nonces; no inline scripts/styles unless nonce-injected.
  - HTML encoding by default; ban un-sanitized raw HTML outputs.
- Cookies and session: Secure, HttpOnly, SameSite=Strict; short sliding expirations.
- Data protection: Persist ASP.NET DataProtection keys to disk for stable encryption/signing across restarts.
- POPI alignment: Minimize PII in views/exports; log export metadata; redact for non-Charter roles; do not log secrets/PII.
- Ignore SQLite encryption (rely on filesystem ACLs, controlled access, and DataProtection for cookies/tokens).
- Rate/resource limits: Global rate limiter policies, max request size, server-side pagination, query timeouts, and enforced cancellation.

## Cross-DB Identity and Join Strategy
- Primary key for linkage: normalized email (lowercase, trimmed); fallback to additional identifiers when available.
- Per-request transient in-memory map for normalized identity joins.
- Optional persistent identity link table in SQLite to store resolved email ↔ idnumber mappings to stabilize joins over time and reduce anomalies.
- Mandatory filter bounds and query budget guard to keep in-memory joins small and predictable.

## CPD Logic and Compliance Rules
- Year partition: Config defines the cycle start month; compute a stable [Start, End) window using Africa/Johannesburg time.
- Distinct counting: Per-user distinct CPD course completions within the configured year; de-duplicate re-takes within the same year unless explicitly required otherwise.
- Fourth-completion spotlight: Include user identity, course, and completion timestamp; handle category recodes and email changes; tie-break using earliest completion date.
- CPD category filter: Central configuration of Moodle category names/IDs; validated at startup.
- Anomaly handling: Flag duplicate completions, email mismatches, and mapping gaps for manual review.

## Reporting, Dashboards, and UI/UX
- Server-driven charts and tables; no client aggregation; views receive pre-aggregated view models only.
- Default filters applied everywhere (e.g., last 90 days); CPD categories preselected by default for REBOSA and PPRA roles.
- All tables are paginated server-side with sorting and filtering performed in queries.
- Accessibility: Provide data table alternatives for charts; ARIA labels; keyboard navigation; focus management after AJAX updates.
- Theme: Charter colors; Bootstrap responsive grid; SB Admin 2 components; semantic markup.

## Accessibility and UX Polish
- Skip links: Provide "Skip to content" and "Skip to filters" links for keyboard users.
- Empty states: Clear "No results" messaging with guidance to adjust filters; display applied filter summary.
- Retry prompts: For transient data-source errors, present non-blocking retry actions and preserve current filters.
- Focus management: Maintain focus after AJAX updates and announce changes using ARIA live regions.
- Keyboard support: Ensure full tab order coverage and visible focus indicators; Escape closes modals.

## Exports
- Default to CSV streaming for large datasets to control memory and latency.
- XLSX generation gated by a strict size cap (for example, 100k rows) to avoid memory pressure; user is warned before running.
- Export Safety Router sequence: role check → column allow-list → row cap → redaction map → proceed or reject.
- Watermarking: Include requester identity, correlation ID, and filter JSON in the filename and persist the same metadata in `ExportLog`.
- Role-based redactions and column visibility enforced consistently across datasets.

## Reliability, Errors, and Limits
- Global exception handling with correlation IDs and safe user messaging.
- Health endpoints for liveness and dependency checks (application and DB connectivity).
- Timeouts and cancellation tokens propagated from controllers through services and Dapper/EF queries.
- Query budget guardrails and server-side pagination to avoid runaway queries.

## Observability and Audit
- Structured logging with correlation IDs; avoid PII in logs.
- Audit trail records: logins, approvals, export requests and results (row counts, filters, requester, correlation).
- Minimal admin audit views for anomaly review and export history.

## Data Access Strategy
- EF Core (SQLite): Identity, approvals, audit, export logs, system config; async APIs only.
- Dapper (MariaDB): Reporting queries and projections; parameterized queries only; materialize only necessary columns; use disciplined projections.
- In-app joins between source datasets occur over normalized identifiers after bounded, filtered retrieval.

## Validation and Safety
- FluentValidation for input models including ReportFilter, registration, and approvals.
- Deny unconstrained queries (missing date bounds, categories, or exceeding row estimates).
- Export Safety Router enforces column allow-lists, row caps, and redaction before any export proceeds.

## Email Flows
- Email verification and notifications require SMTP. Use an organization relay or explicitly allow SES as a controlled dependency and document it.
- Approval decisions trigger notifications; all emails are templated, minimal PII.

## Performance Strategy
- Always filter by timeframe and category; project only required fields.
- Use pagination for all data tables and streaming for large exports.
- Validate source indexes with DBAs where applicable; avoid N+1 retrieval.
- No caching initially; evaluate targeted server-side caching only if telemetry indicates need.

## Testing Strategy
- Unit tests: authorization policies, CPD computations (distinct counting and fourth-completion spotlight), export redaction logic, filter validators.
- Integration tests: controller RBAC, Dapper queries against a seeded test MariaDB, EF Core against SQLite; export safety checks.
- E2E smoke: onboarding → login → dashboard load → filter apply → export trigger → download verified.
- Data correctness: cross-check sample results against live data with read-only credentials.

## DevOps and Environments
- Config via environment-specific settings; secrets via environment variables or parameter store; no secrets in source.
- IIS hosting on AWS EC2 t3.micro with HTTPS and HSTS; ASP.NET Core Module; URL rewrite for HTTPS redirect.
- Persist DataProtection keys to disk; configure cookie policies globally.
- Backups for SQLite and logs (filesystem snapshot strategy); source DB backups remain the responsibility of their owners.

## Implementation Roadmap
- Phase 1: Authentication, registration, approvals; RBAC; baseline Charter dashboard (sales → enrollments → completions); CSV export with safety checks.
- Phase 2: CPD-only flows and PPRA dashboard with fourth-completion spotlight; REBOSA dashboards; export variants and redaction.
- Phase 3: Accessibility polish, CSP nonces across views, audit views, performance hardening, query budget guard.
- Phase 4: Ops hardening (rate limiting, request size limits, health checks), documentation finalization, and production readiness review.

## Deliverables and Structure (descriptive)
- Constants: Roles, Policies, ExportColumns (centralized names to eliminate magic strings).
- Middleware/Services: Export Safety Router; FilterValidationService; CSP with nonces; RateLimiter and RequestSizeLimit policies; correlation ID enrichment.
- Data: AppDbContext (SQLite, EF Core); Dapper query modules for Moodle and WooCommerce.
- Application Services: Dashboard, Compliance, Export, Approval, Filter Validation.
- View Models: Server-prepared chart/table data; pre-aggregated and paginated results.
- Logs and Audit: AuditLog, ExportLog, Approvals.

## Governance and Rules Alignment
- Enforced by `.cursor` MASTER-CURSORRULES and Context rules:
  - Clean controllers; business logic in services; repositories for data access.
  - Async end-to-end with CancellationToken propagation; no static state.
  - Server-side-first, live-data-only; zero client aggregation.
  - Security-first: CSRF, CSP with nonces, role/policy constants, export safety, POPI-aligned redaction.
  - Quality gate: maintain small, readable methods; unit/integration coverage for core services.

## Risks and Mitigations
- Edwiser mapping drift: add verification checks and anomaly surfacing; maintain a mapping review report.
- Identity mismatches: normalization and optional identity link table; anomaly logging with review workflow.
- Large data requests: mandatory bounds, rate limiting, request size caps, query budget refusal.
- CPD cycle ambiguity: configurable start month; documented defaults; explicit timezone handling.

---

This document is the authoritative implementation guide for the Charter Reporter App. It operationalizes the requirements and the `.cursor` rules into a concrete, secure, and reliable delivery plan without exposing code.
