using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Miner.TxMemPool
{
    public interface ITxPool
    {
        ///<summary>
        /// add tx to tmp pool
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task<TxValidation.TxInsertionAndBroadcastingError> AddTxAsync(Transaction tx, bool validateReference = true);

        /// <summary>
        /// remove a tx from collection not pool
        /// </summary>
        /// <param name="txHash"></param>
        void RemoveAsync(Hash txHash);

        /// <summary>
        /// return ready txs can be executed 
        /// </summary>
        /// <returns></returns>
        Task<List<Transaction>> GetReadyTxsAsync(Round currentRoundInfo = null, double intervals = 150);

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
        bool TryGetTx(Hash txHash, out Transaction tx);

        /// <summary>
        /// open transaction pool
        /// </summary>
        void Start();

        /// <summary>
        /// close transaction pool
        /// </summary>
        Task Stop();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="start"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
//        Task<bool> GetReadyTxsAsync(Hash addr, ulong start, ulong ids);

        /// <summary>
        /// roll back
        /// </summary>
        /// <returns></returns>
        Task Revert(List<Transaction> txsOut);

        void SetBlockVolume(int minimal, int maximal);
    }
}