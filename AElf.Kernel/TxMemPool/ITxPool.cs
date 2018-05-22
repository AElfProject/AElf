using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AElf.Kernel.TxMemPool
{
    public interface ITxPool
    {
        /// <summary>
        /// queue txs from tmp to waiting
        /// </summary>
        /// <param name="tx"></param>
        void QueueTxs(HashSet<ITransaction> tx);
        
        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        bool DiscardTx(Hash txHash);

        /// <summary>
        /// remove executed txs from executable
        /// </summary>
        List<ITransaction> RemoveExecutedTxs();

        /// <summary>
        /// promote txs from waiting to executable
        /// </summary>
        /// <param name="addrs"></param>
        void Promote(List<Hash> addrs = null);
        
        /// <summary>
        /// return ready tx count 
        /// </summary>
        //ulong ReadyTxCount { get; }
        
        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        ulong Size { get; }

        /// <summary>
        /// return tx list can be executed
        /// </summary>
        List<ITransaction> ReadyTxs(ulong limit);

        /// <summary>
        /// threshold for entering pool
        /// </summary>
        ulong EntryThreshold { get; }

        /// <summary>
        /// txs can be enqueued to waiting list if Enqueueable is true,
        /// otherwise they can't be.
        /// </summary>
        bool Enqueueable { get; set; }
        
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
        /// cache incrementId for account
        /// </summary>
        /// <returns></returns>
        Dictionary<Hash, ulong> Nonces { get; }

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        bool GetTx(Hash txHash, out ITransaction tx);

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        ITransaction GetTx(Hash txHash);

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
        //ulong TmpSize { get; }
    }
}