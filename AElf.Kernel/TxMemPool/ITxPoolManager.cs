using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.TxMemPool
{
    public interface ITxPoolManager
    {
        
        ///<summary>
        /// add tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        Task<bool> AddTransaction(ITransaction tx);
        
        /// <summary>
        /// add multi txs
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        Task<bool> AddTransactions(List<ITransaction> txs);
        
        /// <summary>
        /// remove a tx
        /// </summary>
        /// <param name="txHash"></param>
        Task Remove(Hash txHash);

        /// <summary>
        /// remove tx with worst price
        /// </summary>
        Task RemoveTxAsWorstFee();

        /// <summary>
        /// Remove transactions from mempool already in block
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <returns></returns>
        Task RemoveTxsExecuted(ulong blockHeight);

        /// <summary>
        /// Remove invalid transactions from mempool
        /// not in executable and waiting list 
        /// </summary>
        /// <returns></returns>
        Task RemoveTxsInValid();
        
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
        Task<bool> GetTransaction(Hash txHash, out ITransaction tx);

        /// <summary>
        /// clear tx pool
        /// </summary>
        /// <returns></returns>
        Task Clear();

        /// <summary>
        /// persistent Tx pool to storage
        /// </summary>
        /// <returns></returns>
        Task SavePool();
    }
}