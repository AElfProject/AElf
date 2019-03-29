using System;
using System.Diagnostics;
using AElf.Common;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Xunit.Abstractions;

namespace AElf.Database.Benches
{
    public class StorageKeyTests
    {
        public StorageKeyTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        
        private Counter _counter;
        private Hash _hash;

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _counter = context.GetCounter("TestCounter");
            _hash = Hash.FromString("FromStringFromStringFromStringFromString");
        }

        [NBenchFact]
        [PerfBenchmark(Description = "Test to ensure that a minimal throughput test can be rapidly executed.", 
            NumberOfIterations = 3, RunMode = RunMode.Throughput, 
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        //[MemoryAssertion(MemoryMetric.TotalBytesAllocated, MustBe.LessThanOrEqualTo, ByteConstants.ThirtyTwoKb)]
        [GcTotalAssertion(GcMetric.TotalCollections, GcGeneration.Gen2, MustBe.ExactlyEqualTo, 0.0d)]
        public void HashToStorageKey()
        {
            _hash.ToHex();
            _counter.Increment();
        }
        
        [NBenchFact]
        [PerfBenchmark(Description = "Test to ensure that a minimal throughput test can be rapidly executed.", 
            NumberOfIterations = 3, RunMode = RunMode.Throughput, 
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        //[MemoryAssertion(MemoryMetric.TotalBytesAllocated, MustBe.LessThanOrEqualTo, ByteConstants.ThirtyTwoKb)]
        [GcTotalAssertion(GcMetric.TotalCollections, GcGeneration.Gen2, MustBe.ExactlyEqualTo, 0.0d)]
        public void HashToUtf8()
        {
            _hash.Value.ToStringUtf8();
            _counter.Increment();
        }
        
        [NBenchFact]
        [PerfBenchmark(Description = "Test to ensure that a minimal throughput test can be rapidly executed.", 
            NumberOfIterations = 3, RunMode = RunMode.Throughput, 
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        //[MemoryAssertion(MemoryMetric.TotalBytesAllocated, MustBe.LessThanOrEqualTo, ByteConstants.ThirtyTwoKb)]
        [GcTotalAssertion(GcMetric.TotalCollections, GcGeneration.Gen2, MustBe.ExactlyEqualTo, 0.0d)]
        public void HashToBase64()
        {
            _hash.Value.ToBase64();
            _counter.Increment();
        }
        
        [NBenchFact]
        [PerfBenchmark(Description = "Test to ensure that a minimal throughput test can be rapidly executed.", 
            NumberOfIterations = 3, RunMode = RunMode.Throughput, 
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, .0d)]
        //[MemoryAssertion(MemoryMetric.TotalBytesAllocated, MustBe.LessThanOrEqualTo, ByteConstants.ThirtyTwoKb)]
        [GcTotalAssertion(GcMetric.TotalCollections, GcGeneration.Gen2, MustBe.ExactlyEqualTo, 0.0d)]
        public void HashToBytes()
        {
            _hash.Value.ToByteArray();
            _counter.Increment();
        }

        [PerfCleanup]
        public void Cleanup(){
            // does nothing
        }
    }
}