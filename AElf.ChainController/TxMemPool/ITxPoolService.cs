using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.ChainController
{
    public interface ITxPoolService
    {
        ///<summary>
        /// add tx to tmp pool
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task<TxValidation.TxInsertionAndBroadcastingError> AddTxAsync(ITransaction tx);

        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        Task RemoveAsync(Hash txHash);

        /// <summary>
        /// remove tx with worst price
        /// </summary>
        Task RemoveTxWithWorstFeeAsync();

        /// <summary>
        /// return ready txs can be executed 
        /// </summary>
        /// <returns></returns>
        Task<List<ITransaction>> GetReadyTxsAsync();

        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        Task<ulong> GetPoolSize();

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        bool TryGetTx(Hash txHash, out ITransaction tx);

        /// <summary>
        /// Given a block this method will return the blocks transactions
        /// that are not currently in the pool.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        List<Hash> GetMissingTransactions(IBlock block);

        /// <summary>
        /// return size of waiting list
        /// </summary>
        /// <returns></returns>
        Task<ulong> GetWaitingSizeAsync();

        /// <summary>
        /// return size of executable list
        /// </summary>
        /// <returns></returns>
        Task<ulong> GetExecutableSizeAsync();
        
        /// <summary>
        /// return tmp pool size
        /// </summary>
        /// <returns></returns>
        //Task<ulong> GetTmpSizeAsync();

        /// <summary>
        /// Reset Enqueueable to true 
        /// update account IncrementId,
        /// which happens a block generated 
        /// </summary>
        /// <returns></returns>
        Task UpdateAccountContext(HashSet<Hash> txResultList);
        
        /// <summary>
        /// open transaction pool
        /// </summary>
        void Start();

        /// <summary>
        /// close transaction pool
        /// </summary>
        Task Stop();

        Task<ulong> GetIncrementId(Hash addr, bool isDPoS = false);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="start"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<bool> GetReadyTxsAsync(Hash addr, ulong start, ulong ids);

        /// <summary>
        /// roll back
        /// </summary>
        /// <returns></returns>
        Task RollBack(List<ITransaction> txsOut);

        void SetBlockVolume(ulong minimal, ulong maximal);
    }
}