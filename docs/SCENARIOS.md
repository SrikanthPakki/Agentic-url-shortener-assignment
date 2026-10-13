# Required Scenarios

## Scenario A — Greenfield
**Prompt:** “Build a URL shortener with click analytics.”

**Decomposition:** define contracts → design mapping and analytics model → implement create/lookup/redirect → validate inputs and expiry → test redirect/analytics → document setup → release review.

**Expected orchestration:** requirements, plan, design, human approval, implementation stage, parallel tests/docs, release gate.

**Validation:** valid URL accepted; malformed URL rejected; redirect resolves; click count increments; unknown/expired code returns 404.

**Trade-off:** in-memory store keeps the prototype simple but is not durable.

## Scenario B — Brownfield
**Prompt:** “Add analytics to an existing URL-shortener API without breaking clients.”

**Brownfield reasoning:** inspect current routes/contracts first; identify the URL mapping store and redirect flow as impacted modules; add analytics as a separate read endpoint; avoid changing existing create/redirect response contracts; add regression tests.

**Expected orchestration:** dependency/impact analysis → compatibility decision → approval → implementation plan → regression + analytics tests in parallel with docs → release review.

**Validation:** existing create and redirect behavior remains unchanged; analytics returns count and last-click timestamp; unknown codes return 404.

**Assumption:** this repository is a greenfield sample, so the brownfield scenario demonstrates the reasoning plan rather than claiming a real legacy repository was modified.

## Scenario C — Ambiguous requirement
**Prompt:** “Make links safer and keep analytics as needed.”

**Ambiguities:** what “safer” means (malware/domain blocklist, abuse limits, private links); which analytics fields are needed; retention duration; user/IP privacy; who can view analytics; acceptable latency.

**Behavior:** stop before implementation and request product/security decisions. Do not invent retention or access-control policy.

**Acceptance questions:**
1. Should links expire by default? If so, after how long?
2. Which analytics fields may be stored, and for how long?
3. Are custom aliases required, and who can create them?
4. Which domains are prohibited?
5. Is analytics public or authenticated?

**Expected orchestration:** normalize → detect ambiguity → safe-stop → return questions and risks → resume only after approved clarification.
