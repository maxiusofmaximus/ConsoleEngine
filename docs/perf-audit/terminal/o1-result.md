# O1 result — Windows dedicated blocking-read thread

> **Evidence-first.** Measured deltas, real console host. Compared against the baseline
> in `baseline.md`. Sources:
> - baseline `5bb1c6c` → `artifacts/.../terminal-bench-20260602-070655-cde20981.jsonl`
> - burst baseline `0d08027` → `artifacts/.../terminal-bench-20260602-074931-be758135.jsonl`
> - **O1 `125891a`** → `artifacts/.../terminal-bench-20260602-080401-c918cdf8.jsonl`

## The change

`WindowsConPtyBackend` previously read with `await _outStream.ReadAsync(...)` on a
**synchronous** pipe `FileStream`. .NET implements that by offloading each read to the
thread pool wrapped in a `Task` — ~163 B allocated per read plus a thread-pool hop. O1
replaces it with a dedicated background thread doing blocking synchronous `Read`, the same
pattern `PosixPtyBackend` already uses (the two backends are now symmetric). EOF
(ConPTY releasing the write end on `ClosePseudoConsole`) unblocks the read, preserving the
teardown contract. The 4 KB buffer is unchanged (E1 dropped — see below).

## Measured deltas

| scenario · metric | baseline | O1 (125891a) | Δ |
|---|---|---|---|
| throughput · alloc_per_byte | 2.369 | 0.0178 | **−99.2%** |
| throughput · alloc_bytes | 3 269 568 | 24 600 | −99.2% |
| throughput · throughput_mb_s | 0.713 | 0.841 | +18.0% |
| throughput · cpu_ms_total | 1031.25 | 156.25 | −84.8% |
| burst · alloc_per_byte¹ | 0.826 | 0.0034 | **−99.6%** |
| burst · throughput_mb_s¹ | 2.004 | 2.156 | +7.6% |
| burst · cpu_ms_total¹ | 437.5 | 93.75 | −78.6% |
| concurrent · throughput_mb_s | 3.093 | 4.265 | **+37.9%** |
| concurrent · alloc_bytes | 4 049 264 | 180 640 | −95.5% |
| concurrent · ms_per_session | 42.58 | 30.88 | −27.5% |
| concurrent · failures | 0 | 0 | no deadlock (12 read threads) |
| startup · latency_median_ms | 36.44 | 31.99 | −12.2% |
| startup · alloc_per_spawn_bytes | 8 644 | 6 992 | −19.1% |
| write_roundtrip · rtt_median_ms | 0.488 | 0.469 | −3.7% (noise; write path untouched) |
| resize · resize_median_ms | 0.0009 | 0.0008 | noise |
| dispose · dispose_median_ms | 0.190 | 0.171 | noise |

¹ burst compared against `0d08027` (same scenario, backend differs only by O1).
GC gen0/1/2 = 0 in every scenario, before and after.

## Headline

- **Read-path allocation effectively eliminated**: 2.37 → 0.018 B/byte (−99.2%).
- **CPU on the read path cut by ~80%** (no per-read thread-pool churn).
- **Concurrency improved +37.9%** with zero stability cost — 12 concurrent sessions, each
  with its own read thread, completed with 0 failures and no dispose deadlock.
- Latency paths (write/resize/dispose) unchanged, as expected.

## E1 (read buffer 4096 → 65536): DROPPED on evidence

`burst_throughput` (a deliberate fast flood on a wide terminal) showed avg **193 B/read**;
solving avg = 193(1−x) + 4096x ⇒ only **~0.08%** of reads saturate the 4 KB buffer even
under flood. Throughput is bound by the producer + ConPTY rendering, not our buffer.
Enlarging it cannot move any measured metric, so E1 is not applied.
