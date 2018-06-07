using System;
using System.Threading.Tasks;

namespace AElf.Common.Synchronisation
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
}