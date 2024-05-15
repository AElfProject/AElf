using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace AElf.Kernel;

[Dependency(ServiceLifetime.Singleton)]
public class UmpInterceptor : AbpInterceptor
{
    private readonly Meter _meter;
    private readonly Dictionary<string, Histogram<long>> _histogramMapCache = new Dictionary<string, Histogram<long>>();


    public UmpInterceptor()
    {
        _meter = _meter = new Meter("AElf", "1.0.0");
    }

    public override async Task InterceptAsync(IAbpMethodInvocation invocation)
    {
        var histogram = GetHistogram(invocation);

        var stopwatch = Stopwatch.StartNew();
        stopwatch.Start();

        await invocation.ProceedAsync();

        stopwatch.Stop();

        histogram.Record(stopwatch.ElapsedMilliseconds);

        // Counter<long> couter = _meter.CreateCounter<long>(
        //     name: className + "_" + methodName + "_count",
        //     description: "Counter for method execution times"
        // );
        // couter.Add(1);
    }

    private Histogram<long> GetHistogram(IAbpMethodInvocation invocation)
    {
        var methodName = invocation.Method.Name;
        var className = invocation.TargetObject.GetType().Name;

        var rtKey = className + "_" + methodName + "_rt";

        if (_histogramMapCache.TryGetValue(rtKey, out var rtKeyCache))
        {
            return rtKeyCache;
        }
        else
        {
            var histogram = _meter.CreateHistogram<long>(
                name: rtKey,
                description: "Histogram for method execution time",
                unit: "ms"
            );
            _histogramMapCache.Add(rtKey, histogram);
            return histogram;
        }
    }
}