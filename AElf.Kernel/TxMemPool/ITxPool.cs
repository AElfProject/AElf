using System.Collections.Generic;
using AElf.Kernel.TxMemPool;

namespace AElf.Kernel.TxMemPool
{
    public interface ITxPool
    {
        /// <summary>
        /// add tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool AddTx(ITransaction tx);

        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="tx"></param>
        bool DisgardTx(Hash txHash);

        /// <summary>
        /// promote txs from waiting to executable
        /// </summary>
        /// <param name="addrs"></param>
        void Promote(List<Hash> addrs);

        /// <summary>
        /// validate a tx before added to pool    
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool ValidateTx(ITransaction tx);

        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        ulong Size { get; }
        
        List<ITransaction> Ready { get; }

        /// <summary>
        /// minimal fee needed
        /// </summary>
        /// <returns></returns>
        Fee MinimalFee { get; }

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool GetTransaction(Hash txHash, out ITransaction tx);

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        ITransaction GetTransaction(Hash txHash);

        /// <summary>
        /// clear all txs in pool
        /// </summary>
        void ClearAll();

        /// <summary>
        /// clear all txs in waiting list
        /// </summary>
        void ClearWaiting();

        /// <summary>
        /// clear all txs in executable list
        /// </summary>
        void ClearExecutable();
    }
}