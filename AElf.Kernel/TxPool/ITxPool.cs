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
        /// add multi txs
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        bool AddTxs(List<ITransaction> txs);
        
        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        void Remove(Hash txHash);

        /// <summary>
        /// remove tx with worst price
        /// </summary>
        void RemoveTxAsWorstPrice();
        
        /// <summary>
        /// validate a tx before added to pool
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool Validate(ITransaction tx);

        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        ulong PoolSize();

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool GetTransaction(Hash txHash, out ITransaction tx);
    }
}