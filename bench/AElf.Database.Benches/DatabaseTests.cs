using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AElf.BenchBase;
using Microsoft.Extensions.Logging;
using NBench;
using Pro.NBench.xUnit.XunitExtensions;
using Volo.Abp.Threading;
using Xunit.Abstractions;

namespace AElf.Database.Benches
{
    public class DatabaseTests : BenchBaseTest<DatabaseBenchAElfModule>
    {
        public DatabaseTests(ITestOutputHelper output)
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new XunitTraceListener(output));
            
            SetTestOutputHelper(output);
            
            GetService<ILogger<DatabaseTests>>().LogWarning("Hello");
        }
        

        private Counter _counter;

        private IKeyValueDatabase<DbContext> _memoryDatabase;
        private DbContext _dbContext;

        private byte[] _testBytes = "BenchmarkContextBenchmarkContextBenchmarkContextBenchmarkContext".GetBytes();

        [PerfSetup]
        public void Setup(BenchmarkContext context)
        {
            _counter = context.GetCounter("TestCounter");

            _dbContext = this.GetService<DbContext>();

            _memoryDatabase = _dbContext.Database;
            _memoryDatabase.SetAsync("hello", _testBytes);
            
            
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 3, RunMode = RunMode.Throughput,
            RunTimeMilliseconds = 5000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, 0d)]
        public void Set()
        {
            AsyncHelper.RunSync(() => _memoryDatabase.SetAsync("hello", _testBytes));
            _counter.Increment();
        }

        [NBenchFact]
        [PerfBenchmark(NumberOfIterations = 3, RunMode = RunMode.Throughput,
            RunTimeMilliseconds = 5000, TestMode = TestMode.Test)]
        [CounterThroughputAssertion("TestCounter", MustBe.GreaterThan, 0d)]
        public void Get()
        {
            AsyncHelper.RunSync(() => _memoryDatabase.GetAsync("hello"));

            _counter.Increment();
        }

        [PerfCleanup]
        public void Cleanup()
        {
            // does nothing
        }
    }
}