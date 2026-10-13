# Testing, Risks, Trade-offs

## Manual smoke-test checklist
- [ ] `GET /health` returns healthy.
- [ ] Create a URL with HTTPS and receive a short code.
- [ ] Create rejects malformed input and non-HTTP(S) schemes.
- [ ] Create rejects expiry in the past.
- [ ] Lookup returns mapping for a valid code.
- [ ] Redirect reaches original URL.
- [ ] Analytics count increases after redirect.
- [ ] Unknown code returns 404.
- [ ] Orchestrator without approval returns `awaiting_approval`.
- [ ] Approved greenfield run reports tests/docs and release-review status.
- [ ] Brownfield run includes compatibility decision.
- [ ] Ambiguous run returns `needs_clarification` and does not continue.

## Risk register
| Risk | Impact | Mitigation / next step |
|---|---|---|
| In-memory persistence | Data loss/restart resets mappings | Use a durable database and backups |
| Short-code collision | Failed allocation | Random code plus uniqueness check; add bounded collision retry and DB unique index |
| Abuse/phishing | Brand and user harm | Domain reputation checks, reporting, rate limits, abuse response workflow |
| Analytics privacy | Personal data exposure | Minimize IP data, define retention, access controls, privacy review |
| Agent hallucination | Incorrect code or decisions | Typed schemas, deterministic validators, test gates, human approval |
| Tool side effects | Unsafe releases | Sandboxed execution, least-privilege credentials, approval and rollback plan |
| Prompt injection in requirements/repo | Policy bypass | Treat inputs as data, isolate tool permissions, enforce policy outside model |
| Unobserved failures | Slow incident response | Structured logs, traces, metrics, alerts and run-level correlation IDs |

## Trade-offs
- Minimal API + in-memory store makes local demonstration fast; it is not production persistence.
- Random short codes reduce guessability compared with sequential IDs but do not replace access controls.
- Analytics are intentionally basic; IP address is observed transiently in memory and should be reviewed before production retention.
- The orchestration engine is deterministic to make governance visible and testable. It is not presented as a full autonomous coding agent.
