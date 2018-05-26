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
        Task<bool> AddTxAsync(ITransaction tx);
        
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
        Task<List<ITransaction>> GetReadyTxsAsync(ulong limit);

        /// <summary>
        /// promote txs from waiting list to executable
        /// return true if promotable
        /// otherwis return true
        /// </summary>
        /// <returns></returns>
        Task<bool> PromoteAsync();
        
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
        ITransaction GetTx(Hash txHash);

        /// <summary>
        /// clear tx pool
        /// </summary>
        /// <returns></returns>
        Task ClearAsync();

        /// <summary>
        /// persistent Tx pool to storage
        /// </summary>
        /// <returns></returns>
        Task SavePoolAsync();

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
        Task ResetAndUpdate(List<TransactionResult> txResultList);
        
        /// <summary>
        /// open transaction pool
        /// </summary>
        void Start();

        /// <summary>
        /// close transaction pool
        /// </summary>
        Task Stop();
        
    }
}