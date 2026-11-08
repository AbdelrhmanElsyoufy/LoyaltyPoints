# Handoff

Decisions: the singleton in-memory store serializes award processing and wallet snapshots with one `SemaphoreSlim`. Identity lookup, wallet validation, balance change, and award recording therefore share one critical section, which guarantees exactly one `201` per identity under concurrency. An accepted award stores a generated GUID; a matching retry returns that record unchanged. Wallet reads return a copy, so callers never see a half-finished update. Error precedence is `400`, then identity check (`200`/`409`), then wallet lookup (`404`).

Checks executed: `dotnet test --nologo` ([N] passed, 0 failed). Tests cover validation, retries, conflicts (different points and different wallet), case-sensitive identity, unknown wallet not consuming the identity, wallet lookup, and concurrent matching, distinct, and conflicting requests. Runtime: .NET SDK 10.0.301. Reset: restart the process.

Limitations: one global lock limits throughput (per-wallet or per-identity locking would scale better); state is process-local and lost on restart; not suitable for multiple service instances or durable accounting. Authentication, persistence, and deployment are outside the assessment scope.

Time spent: approximately [X] minutes.
