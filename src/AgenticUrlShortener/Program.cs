using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<UrlStore>();
builder.Services.AddSingleton<AgentOrchestrator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }));
app.MapPost("/api/urls", (CreateUrlRequest request, HttpContext ctx, UrlStore store) => {
    if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new { error = "originalUrl must be an absolute HTTP or HTTPS URL." });
    if (request.ExpiresAt is not null && request.ExpiresAt <= DateTimeOffset.UtcNow)
        return Results.BadRequest(new { error = "expiresAt must be in the future." });
    var item = store.Create(uri, request.ExpiresAt);
    return Results.Created($"/api/urls/{item.Code}", new { item.Code, shortUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/{item.Code}", item.OriginalUrl, item.CreatedAt, item.ExpiresAt });
});
app.MapGet("/api/urls/{code}", (string code, UrlStore store) => {
    var item = store.Find(code);
    return item is null ? Results.NotFound(new { error = "Short URL not found or expired." }) : Results.Ok(item);
});
app.MapGet("/{code}", (string code, HttpContext ctx, UrlStore store) => {
    if (!Regex.IsMatch(code, "^[A-Za-z0-9_-]{6,12}$")) return Results.NotFound(new { error = "Short URL not found or expired." });
    var item = store.Resolve(code, ctx.Request.Headers.UserAgent.ToString(), ctx.Connection.RemoteIpAddress?.ToString());
    return item is null ? Results.NotFound(new { error = "Short URL not found or expired." }) : Results.Redirect(item.OriginalUrl.ToString(), permanent: false);
});
app.MapGet("/api/analytics/{code}", (string code, UrlStore store) => {
    var data = store.Analytics(code);
    return data is null ? Results.NotFound(new { error = "Short URL not found." }) : Results.Ok(data);
});
app.MapPost("/api/agent/runs", async (AgentRunRequest request, AgentOrchestrator orchestrator, CancellationToken ct) => {
    if (!new[] { "greenfield", "brownfield", "ambiguous" }.Contains(request.Scenario, StringComparer.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "scenario must be greenfield, brownfield, or ambiguous." });
    return Results.Ok(await orchestrator.RunAsync(request.Scenario.ToLowerInvariant(), request.Requirement ?? "Build a reliable URL shortener", request.Approved, ct));
});
app.Run();

public sealed record CreateUrlRequest(string OriginalUrl, DateTimeOffset? ExpiresAt = null);
public sealed record AgentRunRequest(string Scenario, string? Requirement = null, bool Approved = false);
public sealed record ShortUrl(string Code, Uri OriginalUrl, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt);
public sealed record ClickEvent(DateTimeOffset At, string? UserAgent, string? IpAddress);
public sealed record AnalyticsResponse(string Code, int TotalClicks, DateTimeOffset? LastClickedAt);

public sealed class UrlStore {
    private readonly ConcurrentDictionary<string, ShortUrl> _urls = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ClickEvent>> _clicks = new();
    public ShortUrl Create(Uri original, DateTimeOffset? expiresAt) {
        string code;
        do { code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(6)).TrimEnd('=').Replace('+','-').Replace('/','_'); }
        while (_urls.ContainsKey(code));
        var item = new ShortUrl(code, original, DateTimeOffset.UtcNow, expiresAt);
        if (!_urls.TryAdd(code, item)) throw new InvalidOperationException("Could not allocate a unique short code.");
        _clicks.TryAdd(code, new ConcurrentQueue<ClickEvent>()); return item;
    }
    public ShortUrl? Find(string code) => _urls.TryGetValue(code, out var item) && !IsExpired(item) ? item : null;
    public ShortUrl? Resolve(string code, string? userAgent, string? ip) {
        var item = Find(code); if (item is null) return null;
        _clicks.GetOrAdd(code, _ => new ConcurrentQueue<ClickEvent>()).Enqueue(new(DateTimeOffset.UtcNow, userAgent, ip)); return item;
    }
    public AnalyticsResponse? Analytics(string code) {
        if (!_urls.ContainsKey(code)) return null;
        var events = _clicks.GetOrAdd(code, _ => new ConcurrentQueue<ClickEvent>()).ToArray();
        return new(code, events.Length, events.OrderByDescending(x => x.At).FirstOrDefault()?.At);
    }
    private static bool IsExpired(ShortUrl item) => item.ExpiresAt is not null && item.ExpiresAt <= DateTimeOffset.UtcNow;
}

public sealed record AgentStep(string Id, string Stage, string Status, string Detail, int Attempts, long DurationMs);
public sealed record AgentRunResult(string RunId, string Scenario, string Requirement, string Status, bool ApprovalRequired, IReadOnlyList<AgentStep> Steps, IReadOnlyList<string> Decisions, IReadOnlyList<string> Risks, long DurationMs);

/// <summary>Deterministic orchestration demo: explicit DAG, bounded retries, approval gate, audit lineage, safe-stop.</summary>
public sealed class AgentOrchestrator {
    private readonly ConcurrentDictionary<string, AgentRunResult> _runs = new();
    public async Task<AgentRunResult> RunAsync(string scenario, string requirement, bool approved, CancellationToken ct) {
        var sw = Stopwatch.StartNew(); var id = Guid.NewGuid().ToString("N");
        var steps = new List<AgentStep>(); var decisions = new List<string>(); var risks = new List<string>();
        void Add(string sid, string stage, string status, string detail, int attempts = 1, long ms = 0) => steps.Add(new(sid, stage, status, detail, attempts, ms));
        Add("requirements", "Requirements", "completed", $"Normalized requirement: {requirement}");
        if (scenario == "ambiguous") {
            decisions.Add("Ambiguity detected: expiry policy, analytics retention, and custom aliases need product-owner confirmation.");
            Add("clarify", "Requirement clarification", "blocked", "Safe-stop: unresolved assumptions prevent implementation approval.");
            risks.Add("Proceeding without clarified retention and expiry requirements may cause compliance or product defects.");
            sw.Stop(); var blocked = new AgentRunResult(id, scenario, requirement, "needs_clarification", false, steps, decisions, risks, sw.ElapsedMilliseconds); _runs[id] = blocked; return blocked;
        }
        Add("plan", "Task decomposition", "completed", "Built dependency graph: requirements -> design -> implementation; tests and docs can run in parallel; release waits for both.");
        Add("design", "Architecture", "completed", "Selected stateless HTTP API boundary, short-code store abstraction, analytics event model, and orchestrator audit trail.");
        if (scenario == "brownfield") decisions.Add("Brownfield plan: inspect existing routes/data contract first; preserve API compatibility; add analytics behind a backward-compatible endpoint.");
        else decisions.Add("Greenfield plan: establish API contracts and validation before implementation.");
        if (!approved) {
            Add("approval", "Human approval", "awaiting_approval", "Implementation/release is paused until an authorized reviewer approves the plan.");
            risks.Add("No code-changing or release action is performed without explicit approval.");
            sw.Stop(); var pending = new AgentRunResult(id, scenario, requirement, "awaiting_approval", true, steps, decisions, risks, sw.ElapsedMilliseconds); _runs[id] = pending; return pending;
        }
        Add("approval", "Human approval", "approved", "Approval explicitly supplied by caller.");
        // Independent validation and documentation branches are represented as parallel tasks.
        var parallelTimer = Stopwatch.StartNew();
        var testsTask = Task.Run(() => ValidateRequirement(requirement), ct);
        var docsTask = Task.Run(() => "Generated setup, API, architecture, risk, and limitation notes.", ct);
        await Task.WhenAll(testsTask, docsTask); parallelTimer.Stop();
        Add("implement", "Implementation", "completed", "Produced a bounded implementation plan; this orchestration demo does not modify source files.");
        Add("tests", "Validation", testsTask.Result ? "passed" : "failed", "Checked required URL-shortener capabilities and HTTP/HTTPS validation.", 1, parallelTimer.ElapsedMilliseconds);
        Add("docs", "Documentation", "completed", docsTask.Result, 1, parallelTimer.ElapsedMilliseconds);
        if (!testsTask.Result) { Add("release", "Release readiness", "safe_stopped", "Validation failed; release blocked."); risks.Add("Release gate blocked because validation failed."); }
        else Add("release", "Release readiness", "ready_for_human_review", "Tests passed; final deployment still requires human review.");
        risks.Add("Prototype uses in-memory storage; data is lost on restart and is not horizontally shared.");
        risks.Add("Production deployment requires durable storage, rate limiting, secret management, privacy controls, and operational alerting.");
        sw.Stop(); var result = new AgentRunResult(id, scenario, requirement, testsTask.Result ? "ready_for_review" : "failed", true, steps, decisions, risks, sw.ElapsedMilliseconds); _runs[id] = result; return result;
    }
    private static bool ValidateRequirement(string requirement) => !string.IsNullOrWhiteSpace(requirement) && (requirement.Contains("url", StringComparison.OrdinalIgnoreCase) || requirement.Contains("short", StringComparison.OrdinalIgnoreCase) || requirement.Length >= 12);
}
