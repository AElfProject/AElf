using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AElf;

public class Instrumentation : IDisposable
{
    internal const string ActivitySourceName = "AElf";
    internal const string MeterName = "AElf";
    private readonly Meter _meter;

    public Instrumentation()
    {
        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();
        ActivitySource = new ActivitySource(ActivitySourceName, version);
        _meter = new Meter(MeterName, version);
        ExecutedTxCounter = _meter.CreateCounter<long>("tx.executed", description: "The count of executed txs");
        ReceivedTxCounter = _meter.CreateCounter<long>("tx.received", description: "The count of received txs");
    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> ExecutedTxCounter { get; }
    public Counter<long> ReceivedTxCounter { get; }

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}