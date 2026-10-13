# Agentic Software Engineering System — URL Shortener

A .NET 8 prototype for the interview assignment. It combines a runnable URL-shortener API with a deterministic, auditable orchestration demo that models requirement understanding, task decomposition, approval gates, parallel validation/documentation, safe-stop behavior, and risk reporting.

> Important honesty boundary: the orchestration is a policy-controlled workflow prototype, not an LLM-powered autonomous coding agent. It does not edit source code or deploy releases. This is intentional and documented; an LLM provider and sandboxed tool adapters can be added behind the same interfaces in a production iteration.

## Requirements
- .NET SDK 8.0
- Optional: curl or Postman

## Run locally
```bash
cd src/AgenticUrlShortener
dotnet restore
dotnet run --urls http://localhost:5080
```
Open Swagger at `http://localhost:5080/swagger`.

## API walkthrough
### 1. Health check
`GET http://localhost:5080/health`

### 2. Create a short URL
```bash
curl -X POST http://localhost:5080/api/urls -H "Content-Type: application/json" -d '{"originalUrl":"https://example.com/articles/agentic-systems"}'
```
Copy `code` from the response.

### 3. Redirect
Open `http://localhost:5080/{code}` in a browser. The request redirects to the original HTTP(S) URL and records a click event.

### 4. Look up a short URL
`GET http://localhost:5080/api/urls/{code}`

### 5. Analytics
`GET http://localhost:5080/api/analytics/{code}`

### 6. Demonstrate orchestration
First run without approval; implementation is gated:
```bash
curl -X POST http://localhost:5080/api/agent/runs -H "Content-Type: application/json" -d '{"scenario":"greenfield","requirement":"Build a URL shortener with click analytics"}'
```
Then explicitly approve the plan:
```bash
curl -X POST http://localhost:5080/api/agent/runs -H "Content-Type: application/json" -d '{"scenario":"greenfield","requirement":"Build a URL shortener with click analytics","approved":true}'
```
Try `brownfield` and `ambiguous` as scenario values. The ambiguous scenario safe-stops and asks for clarification.

## Design
- `UrlStore`: in-memory storage abstraction for mappings and click events.
- Minimal API endpoints: create, lookup, redirect, analytics, health, orchestration run.
- `AgentOrchestrator`: explicit named stages, stage status, run ID, decision lineage, approval gate, bounded stage behavior, parallel test/documentation branches, safe-stop, and risk summary.
- Swagger: interactive API documentation.

## Limitations and production follow-ups
1. In-memory storage is non-durable and not shared across instances; replace with SQL Server/PostgreSQL and migrations.
2. Add persistent audit events, correlation IDs, structured logging, metrics, distributed tracing, and dashboards.
3. Add rate limits, abuse detection, domain/IP policy, SSRF-safe preview behavior (not implemented), privacy/retention policy, and secrets management.
4. Add proper unit/integration tests, load tests, and CI pipeline. This environment did not have the .NET SDK installed, so compilation was not run here.
5. The orchestrator is deterministic and illustrative; connect an LLM behind typed agent interfaces, validate its structured outputs, sandbox tools, and require human approval for external side effects.
6. Production-grade retries should be per-operation and idempotent; rollback should be implemented for actual side effects. This prototype avoids external side effects and therefore reports safe-stop rather than pretending it performed rollback.

See `docs/` for the submission narrative, architecture, scenarios, validation plan, and demo script.
