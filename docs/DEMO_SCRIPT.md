# Interview Demo Script (8–10 minutes)

1. **Open README / architecture (1 min):** “The objective is not just URL shortening. It is a governed workflow that turns requirements into reviewable engineering outcomes.”
2. **Run service (1 min):** `dotnet run --urls http://localhost:5080`; open Swagger.
3. **Create and redirect (2 min):** create an HTTPS URL, copy code, visit the short link, then call analytics and show click count.
4. **Show approval gate (1 min):** call greenfield orchestration without `approved`; explain that the run pauses instead of executing high-impact work.
5. **Approve greenfield (1 min):** repeat with `approved:true`; point to stage statuses, decisions, risks, and duration.
6. **Brownfield (1 min):** show compatibility-first decision and regression-testing plan.
7. **Ambiguous (1 min):** show safe-stop and clarify that uncertain retention/security requirements should not be guessed.
8. **Close (1 min):** explain limitations honestly: in-memory data, deterministic orchestration demo, no LLM-powered code changes or deployment. Describe how to add durable storage, observability, LLM adapters, sandboxed tools, CI, and real rollback.

## Suggested opening
“I designed this as two connected pieces: a working URL-shortener API and a governed orchestration prototype. The API demonstrates the engineering output; the orchestration demonstrates task decomposition, state, dependency gates, approval, parallel branches, safe-stop behavior, and traceable decisions. I have documented what is implemented versus what would be needed for production.”

## Suggested answer: Why not let the agent deploy automatically?
“Deployment is a high-impact action. I keep it behind a human release gate. The agent can prepare artifacts and evidence, but the policy layer—not the language model—enforces approval. This limits the blast radius of hallucinations, prompt injection, and incorrect assumptions.”
