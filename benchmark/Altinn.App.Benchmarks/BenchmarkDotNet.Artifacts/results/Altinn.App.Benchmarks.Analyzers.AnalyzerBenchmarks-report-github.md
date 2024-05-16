```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3593/23H2/2023Update/SunValley3)
13th Gen Intel Core i7-1370P, 1 CPU, 20 logical and 14 physical cores
.NET SDK 8.0.205
  [Host]     : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2


```
| Method          | UseAdditionalFiles | Mean     | Error     | StdDev    | Median   | Rank | Gen0     | Gen1    | Allocated |
|---------------- |------------------- |---------:|----------:|----------:|---------:|-----:|---------:|--------:|----------:|
| AnalyzeMetadata | False              | 2.804 ms | 0.0905 ms | 0.2582 ms | 2.715 ms |    1 | 125.0000 | 15.6250 |   1.56 MB |
| AnalyzeMetadata | True               | 2.735 ms | 0.0610 ms | 0.1769 ms | 2.711 ms |    1 | 156.2500 | 15.6250 |      2 MB |
