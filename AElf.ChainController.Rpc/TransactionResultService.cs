using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.TxMemPool;

namespace AElf.ChainController.Rpc
{
    public class TransactionResultService : ITransactionResultService
    {
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly ITxHub _txHub;

        public TransactionResultService(ITxHub txHub, ITransactionResultManager transactionResultManager)
        {
            _txHub = txHub;
            _transactionResultManager = transactionResultManager;
        }

        /// <inheritdoc/>
        public async Task<TransactionResult> GetResultAsync(Hash txId)
        {
            // in storage
            var res = await _transactionResultManager.GetTransactionResultAsync(txId);
            if (res != null)
            {
                return res;
            }

            // in tx pool
            if (_txHub.TryGetTx(txId, out var tx))
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
            await _transactionResultManager.AddTransactionResultAsync(res);
        }
    }
}