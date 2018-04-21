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
        bool AddTx(Transaction tx);

        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        bool DisgardTx(Hash txHash);

        /// <summary>
        /// promote txs from waiting to executable
        /// </summary>
        /// <param name="addrs"></param>
        void Promote(List<Hash> addrs);
        
        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        ulong Size { get; }
        
        /// <summary>
        /// return tx list can be executed
        /// </summary>
        List<Transaction> Ready { get; }

        /// <summary>
        /// threshold for entering pool
        /// </summary>
        int EntryThreshold { get; }
        
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
        bool GetTransaction(Hash txHash, out Transaction tx);

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        Transaction GetTransaction(Hash txHash);

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

        /// <summary>
        /// return true if contained in pool, otherwise false
        /// </summary>
        /// <param name="txHash"></param>
        bool Contains(Hash txHash);
        
        /// <summary>
        /// return current pool state: executable count, waiting count
        /// </summary>
        /// <param name="executable">executable count</param>
        /// <param name="waiting">waiting count</param>
        /// <returns></returns>
        void GetPoolStates(out ulong executable, out ulong waiting);
    }
}