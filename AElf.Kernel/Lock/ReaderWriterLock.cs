using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Lock
{ 
    /// <inheritdoc />
    /// <summary>
    /// Initializes a new instance of the object with ability to cancel locked tasks.
    /// </summary>
    public class ReaderWriterLock : ILock
    {
        private static readonly ConcurrentExclusiveSchedulerPair SchedulerPair  =
            new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Environment.ProcessorCount);

        private TaskFactory ConcurrentReader { get; } = new TaskFactory(SchedulerPair.ConcurrentScheduler);

        private TaskFactory ExclusiveWritrer { get; } = new TaskFactory(SchedulerPair.ExclusiveScheduler);

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
        public Task WriteLock(Action action)
        {
            return ExclusiveWritrer.StartNew(action);
        }
    }
}
