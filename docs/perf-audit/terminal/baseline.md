# Terminal backend — measured baseline

> **Evidence-first.** Every number below comes from a real run captured on a real
> console host (ConPTY renders child content only there). Source of truth:
> `artifacts/benchmarks/terminal/terminal-bench-20260602-070655-cde20981.jsonl`.
> Do not edit these numbers by hand — re-run the harness to refresh.

## Run environment

| field | value |
|---|---|
| RunId | 20260602-070655-cde20981 |
| UTC | 2026-06-02T07:06:55Z |
| OS | Microsoft Windows 10.0.26200 (X64) |
| Framework | .NET 8.0.22 · Release |
| Cores | 20 · Machine MAX |
| Git commit | 5bb1c6c (pre-optimization) |
| Backend exercised | `WindowsConPtyBackend` (ConPTY) |

## Measured numbers (baseline = commit 5bb1c6c)

| scenario | result | key metrics |
|---|---|---|
| startup_latency | ok | median **36.44 ms**, p95 42.30 ms, min 27.91 ms · 8 644 B/spawn · 50 iters |
| throughput | ok | **0.713 MB/s** · 1.38 MB in 1 846 ms · 20 061 chunks · **avg 68 B/chunk** · **alloc 2.37 B/byte** (3.27 MB) |
| write_roundtrip | ok | median **0.49 ms**, mean 1.29 ms, p95 1.74 ms, max 15.51 ms · 40/40 ok |
| resize_latency | ok | median **0.0009 ms**, p95 0.0495 ms · 1 000 iters · 56 KB total |
| dispose_latency | ok | median **0.19 ms**, p95 0.27 ms · 15 iters |
| concurrent_stress | ok | **3.09 MB/s** aggregate · 12 sessions × 2 000 lines · 0 failures · 42.6 ms/session · 3.86 MB alloc |

GC: **gen0/1/2 = 0** in every scenario — no collections triggered. Allocation is
real but short-lived enough to stay in the nursery for these run lengths.

## What the data actually says (findings)

1. **Throughput is producer-bound, not backend-bound.**
   `avg_chunk_bytes = 68` and `chunks (20 061) ≈ lines (20 000)` → the OS pipe hands us
   **one echoed line per read**. The 0.713 MB/s figure is `cmd.exe`'s per-line echo rate,
   not our read ceiling. `concurrent_stress` confirms it: 12 parallel sessions reach
   3.09 MB/s aggregate (≈0.26 MB/s each) — the limit scales with session count, i.e. it is
   the child, not our loop.

2. **The read buffer size is irrelevant for this workload.**
   Reads already return ~68 B against a 4 096 B buffer. Enlarging it to 64 KB cannot
   coalesce lines the producer hasn't written yet. → **The previously-planned E1
   (4096→65536) has no measured justification on this evidence.** It would only matter for
   a *bursty* producer (a real AI CLI painting a full frame), which this harness does not
   yet exercise.

3. **The one measurable inefficiency is allocation on the Windows read path: 2.37 B/byte.**
   `WindowsConPtyBackend` opens its pipe ends as **synchronous** `FileStream`s
   (`new FileStream(handle, FileAccess.Read)` — no `isAsync`). `ReadAsync` on a synchronous
   `FileStream` offloads each call to the thread pool wrapped in a `Task`. At 20 061 reads
   that is ~163 B/read of `Task`/closure churn (3.27 MB total), and a thread-pool hop per
   read adds latency that matters for interactive use. This is the highest-value,
   evidence-backed target.

4. **POSIX write path (`PosixPtyBackend.Write`) still does `data.ToArray()` per call** —
   real but tiny (keystroke-sized writes), and not measurable on this Windows run. Worth
   removing for cleanliness/correctness (E2), but it is not a measured hotspot.

## Implication for the optimization plan

The pre-data plan (E1 buffer bump + E2 POSIX zero-copy) is **partially overturned by the
evidence**:

- **E1** → dropped as a "throughput win" (unsupported). May still be applied as cheap
  insurance for bursty producers, but must be labelled unmeasured-by-this-benchmark.
- **E2** → keep, but as a low-risk allocation/cleanliness fix, not a measured win.
- **New O1 (evidence-led)** → make the Windows pipe I/O genuinely asynchronous (overlapped
  pipe + `isAsync` `FileStream`), eliminating the per-read `Task` allocation and the
  thread-pool hop. This is what the 2.37 B/byte number points at.

Each change must be re-measured against this baseline in its own commit before the next.
