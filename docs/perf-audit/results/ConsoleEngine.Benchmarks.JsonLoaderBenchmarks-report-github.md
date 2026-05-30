```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8524)
Unknown processor
.NET SDK 9.0.309
  [Host]   : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method                           | Count | Mean         | Error       | StdDev      | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|--------------------------------- |------ |-------------:|------------:|------------:|------:|--------:|--------:|--------:|----------:|------------:|
| **SceneLoader_LoadAll**              | **1**     |           **NA** |          **NA** |          **NA** |     **?** |       **?** |      **NA** |      **NA** |        **NA** |           **?** |
| MarkdownLocalizationLoader_Parse | 1     |     334.8 ns |     6.61 ns |     9.69 ns |     ? |       ? |  0.1059 |       - |   1.63 KB |           ? |
|                                  |       |              |             |             |       |         |         |         |           |             |
| **SceneLoader_LoadAll**              | **10**    |           **NA** |          **NA** |          **NA** |     **?** |       **?** |      **NA** |      **NA** |        **NA** |           **?** |
| MarkdownLocalizationLoader_Parse | 10    |   1,797.9 ns |    22.26 ns |    20.82 ns |     ? |       ? |  0.5226 |  0.0019 |   8.02 KB |           ? |
|                                  |       |              |             |             |       |         |         |         |           |             |
| **SceneLoader_LoadAll**              | **100**   |           **NA** |          **NA** |          **NA** |     **?** |       **?** |      **NA** |      **NA** |        **NA** |           **?** |
| MarkdownLocalizationLoader_Parse | 100   |  15,680.5 ns |   312.96 ns |   334.86 ns |     ? |       ? |  4.9896 |  0.3204 |  76.58 KB |           ? |
|                                  |       |              |             |             |       |         |         |         |           |             |
| **SceneLoader_LoadAll**              | **1000**  |           **NA** |          **NA** |          **NA** |     **?** |       **?** |      **NA** |      **NA** |        **NA** |           **?** |
| MarkdownLocalizationLoader_Parse | 1000  | 162,523.8 ns | 2,448.51 ns | 2,170.54 ns |     ? |       ? | 51.0254 | 20.2637 |  784.3 KB |           ? |

Benchmarks with issues:
  JsonLoaderBenchmarks.SceneLoader_LoadAll: .NET 8.0(Runtime=.NET 8.0) [Count=1]
  JsonLoaderBenchmarks.SceneLoader_LoadAll: .NET 8.0(Runtime=.NET 8.0) [Count=10]
  JsonLoaderBenchmarks.SceneLoader_LoadAll: .NET 8.0(Runtime=.NET 8.0) [Count=100]
  JsonLoaderBenchmarks.SceneLoader_LoadAll: .NET 8.0(Runtime=.NET 8.0) [Count=1000]
