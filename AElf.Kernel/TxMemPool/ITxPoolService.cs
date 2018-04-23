using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.TxMemPool
{
    public interface ITxPoolService
    {
        
        ///<summary>
        /// add tx to tmp pool
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task<bool> AddTxAsync(Transaction tx);
        
        
        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        Task Remove(Hash txHash);

        /// <summary>
        /// remove tx with worst price
        /// </summary>
        Task RemoveTxWithWorstFee();

        /// <summary>
        /// Remove transactions from mempool already in block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        Task RemoveTxsExecuted(Block block);

        /// <summary>
        /// return ready txs can be executed 
        /// </summary>
        /// <returns></returns>
        Task<List<Transaction>> GetReadyTxs();
        
        /// <summary>
        /// return pool size
        /// </summary>
        /// <returns></returns>
        Task<ulong> GetPoolSize();

        /// <summary>
        /// return a tx alread in pool
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task<bool> GetTxAsync(Hash txHash, out Transaction tx);

        /// <summary>
        /// clear tx pool
        /// </summary>
        /// <returns></returns>
        Task Clear();

        /// <summary>
        /// add txs to storage
        /// </summary>
        /// <param name="txHashes"></param>
        /// <returns></returns>
        Task PersistTxs(IEnumerable<Hash> txHashes);

        /// <summary>
        /// persistent Tx pool to storage
        /// </summary>
        /// <returns></returns>
        Task SavePool();

        Task<ulong> GetWaitingSizeAsync();

        Task<ulong> GetExecutableSizeAsync();
        
        /// <summary>
        /// return tmp pool size
        /// </summary>
        /// <returns></returns>
        Task<ulong> GetTmpSizeAsync();
        
        /// <summary>
        /// open transaction pool
        /// </summary>
        void Start();

        /// <summary>
        /// close transaction pool
        /// </summary>
        void Stop();
    }
}