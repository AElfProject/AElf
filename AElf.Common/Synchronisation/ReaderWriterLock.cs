using System;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Common.Synchronisation
{
    /// <inheritdoc />
    /// <summary>
    /// Initializes a new instance of the object with ability to cancel locked tasks.
    /// </summary>
    public class ReaderWriterLock : ILock
    {
        private readonly ConcurrentExclusiveSchedulerPair SchedulerPair  =
            new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Environment.ProcessorCount);

        public ReaderWriterLock()
        {
            ExclusiveWritrer = new TaskFactory(SchedulerPair.ExclusiveScheduler);
            ConcurrentReader = new TaskFactory(SchedulerPair.ConcurrentScheduler);
        }


        private TaskFactory ConcurrentReader { get; }

        private TaskFactory ExclusiveWritrer { get; }

        /// <inheritdoc />
        public Task<T> ReadLock<T>(Func<T> func)
        {
            return ConcurrentReader.StartNew(func);
        }
        
        /// <inheritdoc />
        public Task<T> WriteLock<T>(Func<T> func)
        {
            return ExclusiveWritrer.StartNew(func);
        }
        
        /// <inheritdoc />
        public Task WriteLock(Action action, CancellationToken token = default(CancellationToken))
        {
            return ExclusiveWritrer.StartNew(action, token);
        }
    }
}