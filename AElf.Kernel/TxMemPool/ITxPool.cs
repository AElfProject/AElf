using System.Collections.Generic;

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
        /// queue txs from tmp to waiting
        /// </summary>
        void QueueTxs();
        
        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        bool DiscardTx(Hash txHash);

        /// <summary>
        /// remove executed txs from executable
        /// </summary>
        List<Transaction> RemoveExecutedTxs();

        /// <summary>
        /// promote txs from waiting to executable
        /// </summary>
        /// <param name="addrs"></param>
        void Promote(List<Hash> addrs = null);
        
        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        ulong Size { get; }

        /// <summary>
        /// return tx list can be executed
        /// </summary>
        List<Transaction> ReadyTxs();

        /// <summary>
        /// threshold for entering pool
        /// </summary>
        ulong EntryThreshold { get; }

        /// <summary>
        /// return chain id for this pool
        /// </summary>
        Hash ChainId { get; }

        /// <summary>
        /// limit size for tx
        /// </summary>
        uint TxLimitSize { get; }
        
        /// <summary>
        /// minimal fee needed
        /// </summary>
        /// <returns></returns>
        ulong  MinimalFee { get; }

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool GetTx(Hash txHash, out Transaction tx);

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        Transaction GetTx(Hash txHash);

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
        /// return waiting list size
        /// </summary>
        /// <returns></returns>
        ulong GetWaitingSize();
        
        /// <summary>
        /// return Executable list size
        /// </summary>
        /// <returns></returns>
        ulong GetExecutableSize();
        
        
        /// <summary>
        /// return Tmp list size
        /// </summary>
        /// <returns></returns>
        ulong TmpSize { get; }
    }
}