``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.2 (18C54) [Darwin 18.2.0]
Intel Core i5-7360U CPU 2.30GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.2.102
  [Host] : .NET Core 2.2.1 (CoreCLR 4.6.27207.03, CoreFX 4.6.27207.03), 64bit RyuJIT DEBUG

Toolchain=InProcessToolchain  

```
|        Method | TransactionCount |           Mean |        Error |        StdDev |         Median |
|-------------- |----------------- |---------------:|-------------:|--------------:|---------------:|
| **MineBlockTest** |                **1** |       **497.3 us** |     **11.62 us** |      **24.00 us** |       **488.7 us** |
| **MineBlockTest** |               **10** |     **2,754.5 us** |     **29.70 us** |      **26.33 us** |     **2,755.5 us** |
| **MineBlockTest** |              **100** |    **25,312.6 us** |    **494.48 us** |     **485.65 us** |    **25,237.4 us** |
| **MineBlockTest** |             **1000** |   **253,498.6 us** |  **1,560.50 us** |   **1,383.34 us** |   **253,031.2 us** |
| **MineBlockTest** |             **3000** |   **749,341.0 us** |  **4,219.35 us** |   **3,740.35 us** |   **747,905.7 us** |
| **MineBlockTest** |             **5000** | **1,370,626.6 us** | **45,694.68 us** | **128,132.79 us** | **1,320,394.9 us** |
