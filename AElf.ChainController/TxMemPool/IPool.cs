using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.ChainController.TxMemPool
{
    public interface IPool
    {
        /// <summary>
        /// queue txs from tmp to waiting
        /// </summary>
        /// <param name="txs"></param>
        void EnQueueTxs(HashSet<ITransaction> txs);
        
        /// <summary>
        /// queue txs from tmp to waiting
        /// </summary>
        /// <param name="tx"></param>
        TxValidation.TxInsertionAndBroadcastingError EnQueueTx(ITransaction tx);
        
        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="tx"></param>
        bool DiscardTx(ITransaction tx);

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
        List<ITransaction> ReadyTxs();
        
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
        /// return size for lists    
        /// </summary>
        /// <param name="waiting"></param>
        /// <param name="executable"></param>
        void GetPoolState(out ulong executable, out ulong waiting);

        /// <summary>
        /// return the biggest incrementId for the addr in the pool
        /// return 0 if the addr not existed
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        ulong GetPendingIncrementId(Hash addr);

        /// <summary>
        /// return ready txs from the addr
        /// incrementIds should be 
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        bool ReadyTxs(Hash addr, ulong start, ulong count);

        /// <summary>
        /// demote all txs from executable to waiting and reset nonces
        /// </summary>
        void Withdraw(Hash addr, ulong withdraw);

        /// <summary>
        /// add nonce if a new address inserted
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="incrementId"></param>
        /// <returns>return true if incrementId inserted</returns>
        bool TrySetNonce(Hash addr, ulong incrementId);
        
        /// <summary>
        /// return incrementId of account
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        ulong? GetNonce(Hash addr);
        
        
        /// <summary>
        /// Transaction type in the pool
        /// </summary>
        TransactionType Type { get; }
    }
}