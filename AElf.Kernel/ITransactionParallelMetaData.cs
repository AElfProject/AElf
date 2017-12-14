using System.Collections.Generic;

namespace AElf.Kernel
{
    public interface ITransactionParallelMetaData
    {
        /// <summary>
        /// Determine whether
        /// </summary>
        /// <returns></returns>
        bool IsParallel();

        /// <summary>
        /// Data conflict means if two transactions have one same hash, they cannot run parallelly
        /// </summary>
        /// <returns></returns>
        IEnumerable<IHash> GetDataConflict();
    }
}