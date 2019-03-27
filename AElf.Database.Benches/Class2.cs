using System;
using System.Diagnostics;
using System.Threading;
using AElf.BenchBase;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Xunit.Abstractions;

namespace AElf.Database.Benches
{
    public class Class2 : BenchBaseTest<DatabaseAElfModule>
    {
        public Class2(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
        }

        class DbContext : KeyValueDbContext<DbContext>
        {
        }

        private Counter _counter;

        private InMemoryDatabase<DbContext> _memoryDatabase;
        private DbContext _dbContext;

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _counter = context.GetCounter("TestCounter");

            
            _memoryDatabase = new InMemoryDatabase<DbContext>();
            _dbContext = new DbContext();
            _dbContext.Database = _memoryDatabase;
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 3, RunMode = RunMode.Throughput,
            RunTimeMilliseconds = 1000, TestMode = TestMode.Test, SkipWarmups = true)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, 0d)]
        public void Set()
        {
            _memoryDatabase.SetAsync("hello", "hello".GetBytes());
            _counter.Increment();
        }

        [PerfCleanup]
        public void Cleanup()
        {
            // does nothing
        }
    }
}