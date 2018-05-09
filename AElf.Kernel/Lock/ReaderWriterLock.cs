using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Lock
{
    /// <summary>
    /// An async reader writer lock for concurrent and exclusive work.
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// parallel on the default scheduler.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> ReadLock<T>(Func<T> func);

        /// <summary>
        /// Delegates calling this method will be done in sequentially
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        Task<T> WriteLock<T>(Func<T> func);


        /// <summary>
        /// for delegate calling without return type
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        Task WriteLock(Action action);
    }

    
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
