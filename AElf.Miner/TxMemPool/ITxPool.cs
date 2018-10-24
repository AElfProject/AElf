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

        void SetBlockVolume(int minimal, int maximal);
    }
}