# PixelArtRenderer Benchmark Baseline — 2026-05-29

> Measured BEFORE any optimization. Run: `dotnet run -c Release -- --filter "*PixelArt*"`

## Results (BenchmarkDotNet 0.14, .NET 8, Release)

| Method | SpriteSize | Mean | Allocated | Gen0 | Gen1 | Gen2 |
|--------|------------|------|-----------|------|------|------|
| ToAnsiString_FromGrid | 32   |  27.1 µs | 126.2 KB | 8.2  | 1.0  | —    |
| ToAnsiString_FromGrid | 64   | 151.9 µs | 490.2 KB | 45.4 | 45.4 | 45.4 |
| ToAnsiString_FromGrid | 128  | 566.7 µs |   1.9 MB | 166  | 166  | 166  |
| ToAnsiString_FromGrid | 256  |   4.0 ms |   7.7 MB | 707  | 671  | 355  |
| ToAnsiString_FromGrid | 512  |  12.1 ms |  29.8 MB | 2375 | 2312 | 984  |
| ToAnsiString_FromGrid | 1024 |  39.7 ms | 119.2 MB | 7785 | 7714 | 2285 |

## Key Findings

### Confirmed Hotspots

1. **Gen2 GC pressure starts at 64×64**: For any sprite ≥ 64px wide, Gen2 collections happen on every call.
   This will cause GC pauses in a real game loop. A 64×64 sprite at 60fps = 9,120 KB/s of Gen2 allocations.

2. **Allocation scales exactly O(n²)** with pixels:
   - 32→64 (4× pixels): 126→490 KB ≈ 3.9× (expected for linear string output)
   - 512→1024 (4× pixels): 30.6→119 MB ≈ 3.9×
   Confirms the algorithm itself is O(n) in pixels, but the string output is large.

3. **1024×1024 is impractical**: 40ms + 119 MB per call. Confirmed as too slow for game loop use.

4. **32×32 (normal sprite)**: 27µs, 126 KB — acceptable but 126 KB per call is high for a hot path.

5. **`string.Create(CultureInfo.InvariantCulture, ...)` allocates intermediate strings**:
   Each pixel column produces one intermediate string from `string.Create`. For a 32×32 sprite:
   - 16 terminal rows × ~32 chars/row × ~3 branches per pixel = many intermediate allocations
   - These inflate Gen0 to 8.2 per 1000 ops even for small sprites.

### Planned Optimization (Exp 21)
Replace `sb.Append(string.Create(CultureInfo.InvariantCulture, $"..."))` with `sb.Append($"...")`.
The C# 10+ interpolated string handler for `StringBuilder.Append` avoids the intermediate string allocation.
Expected reduction: Gen0 from 8.2 → ~2.0 for 32×32; allocation from 126→~60 KB.

### What NOT to Optimize
- BuildSprite (5.9µs, 6.4 KB for 32×32) — fast, no problem
- ToAnsiString_HalfTransparent ≈ same as opaque — transparency branching has no measurable overhead
