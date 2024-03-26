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
        GetChainCounter = _meter.CreateCounter<long>("chain.get", description: "The count of getting chain");
        MiningDurationCounter = _meter.CreateCounter<long>("block.duration", description: "The mining duration");
        TimeslotDurationCounter = _meter.CreateCounter<long>("timeslot.duration", description: "The timeslots duration");
    }

    public ActivitySource ActivitySource { get; }

    public Counter<long> ExecutedTxCounter { get; }
    public Counter<long> ReceivedTxCounter { get; }
    public Counter<long> GetChainCounter { get; }
    public Counter<long> MiningDurationCounter { get; }
    public Counter<long> TimeslotDurationCounter { get; }

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}