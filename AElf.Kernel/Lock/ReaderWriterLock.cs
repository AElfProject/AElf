using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Lock
{
    /// <summary>
    /// An async reader writer lock for concurrent and exclusive work.
    /// </summary>
    public interface ISchedulerLock
    {
        /// <summary>
        /// parallel on the default scheduler.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ReadAsync<T>(Func<T> func);

        /// <summary>
        /// Delegates calling this method will be done in sequentially
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> WriteAsync<T>(Func<T> func);
    }

    
    public class ReaderWriterLock : ISchedulerLock
    {
        /// <summary> Task factory that runs tasks using the concurrent scheduler. Serves as a reader lock.</summary>
        private readonly TaskFactory _concurrentFactory;

        /// <summary> Task factory that runs tasks using the exclusive scheduler. Serves as a writer lock.</summary>
        private readonly TaskFactory _exclusiveFactory;

        /// <summary>
        /// Initializes a new instance of the object with ability to cancel locked tasks.
        /// </summary>
        /// <param name="maxItemsPerTask">Number of exclusive tasks to process before checking concurrent tasks.</param>
        public ReaderWriterLock(int maxItemsPerTask = 5)
        {
            var defaultMaxConcurrencyLevel = Environment.ProcessorCount;
            var defaultMaxItemsPerTask = maxItemsPerTask;
            var schedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, defaultMaxConcurrencyLevel,
                defaultMaxItemsPerTask);
            _concurrentFactory = new TaskFactory(schedulerPair.ConcurrentScheduler);
            _exclusiveFactory = new TaskFactory(schedulerPair.ExclusiveScheduler);
        }
        
        /// <inheritdoc />
        public Task<T> ReadAsync<T>(Func<T> func)
        {
            return _concurrentFactory.StartNew(func);
        }

        /// <inheritdoc />
        public Task<T> WriteAsync<T>(Func<T> func)
        {
            return _exclusiveFactory.StartNew(func);
        }
    }
    
    
}
