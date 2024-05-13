using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace AElf.Kernel;

[Dependency(ServiceLifetime.Transient)]
public class UmpInterceptor : AbpInterceptor
{
    private readonly Meter _meter;

    public UmpInterceptor()
    {
        _meter = _meter = new Meter("AElf", "1.0.0");
    }

    public override async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        var methodName = invocation.Method.Name;
        var className = invocation.TargetObject.GetType().Name;

        Histogram<long> executionTimeHistogram = _meter.CreateHistogram<long>(
            name: className + "." + methodName,
            description: "Histogram for method execution time",
            unit: "ms"
        );
        
        stopwatch.Start();

        await invocation.ProceedAsync();

        stopwatch.Stop();

        executionTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
        
        Counter<long> couter = _meter.CreateCounter<long>(
            name: className + "." + methodName,
            description: "Counter for method execution times"
        );
        couter.Add(1);
    }
}