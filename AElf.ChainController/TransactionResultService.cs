using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Kernel;
using AElf.Kernel.Managers;

namespace AElf.ChainController
{
    public class TransactionResultService : ITransactionResultService
    {
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ITxPoolService _txPoolService;
        private readonly Dictionary<Hash, TransactionResult> _cacheResults = new Dictionary<Hash, TransactionResult>();

        public TransactionResultService(ITxPoolService txPoolService, ITransactionResultManager transactionResultManager)
        {
            _txPoolService = txPoolService;
            _transactionResultManager = transactionResultManager;
        }

        /// <inheritdoc/>
        public async Task<TransactionResult> GetResultAsync(Hash txId)
        {
            /*// found in cache
            if (_cacheResults.TryGetValue(txId, out var res))
            {
                return res;
            }*/

            
            // in storage
            var res = await _transactionResultManager.GetTransactionResultAsync(txId);
            if (res != null)
            {
                _cacheResults[txId] = res;
                return res;
            }

            // in tx pool
            if (_txPoolService.TryGetTx(txId, out var tx))
            {
                return new TransactionResult
                {
                    TransactionId = tx.GetHash(),
                    Status = Status.Pending
                };
            }
            
            // not existed
            return new TransactionResult
            {
                TransactionId = txId,
                Status = Status.NotExisted
            };
        }

        /// <inheritdoc/>
        public async Task AddResultAsync(TransactionResult res)
        {
            _cacheResults[res.TransactionId] = res;
            await _transactionResultManager.AddTransactionResultAsync(res);
        }
    }
}