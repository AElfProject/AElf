using System.Collections.Generic;

namespace AElf.Kernel
{
    public interface ITxPool
    {

        /// add tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool AddTx(ITransaction tx);
        
        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        void Remove(Hash txHash);

        /// <summary>
        /// validate a tx before added to pool    
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="executable"></param>
        /// <returns></returns>
        bool ValidateTx(ITransaction tx);

        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        ulong GetPoolSize();

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool GetTransaction(Hash txHash, out ITransaction tx);
    }
}