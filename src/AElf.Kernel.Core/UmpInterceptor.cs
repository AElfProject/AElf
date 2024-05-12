using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace AElf.Kernel;

[Dependency(ServiceLifetime.Transient)]
public class UmpInterceptor : AbpInterceptor
{
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private readonly Meter _meter;

    public UmpInterceptor()
    {
        _meter = _meter = new Meter("AElf", "1.0.0");
        ;
    }

    public override async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        var methodName = invocation.Method.Name;
        var className = invocation.TargetObject.GetType().Name;

        // 创建直方图
        Histogram<long> executionTimeHistogram = _meter.CreateHistogram<long>(
            name: className + "." + methodName + ".rt",
            description: "Histogram for method execution time",
            unit: "ms"
        );
        _stopwatch.Start();

        await invocation.ProceedAsync();

        _stopwatch.Stop();

        executionTimeHistogram.Record(_stopwatch.ElapsedMilliseconds);
    }
}