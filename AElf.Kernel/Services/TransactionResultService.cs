using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.TxMemPool;

namespace AElf.Kernel.Services
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

        public async Task<TransactionResult> GetResultAsync(Hash txId)
        {
            if (_cacheResults.TryGetValue(txId, out var res))
            {
                return res;
            }

            res = await _transactionResultManager.GetTransactionResultAsync(txId);
            if (res != null)
            {
                _cacheResults[txId] = res;
                return res;
            }

            if (await _txPoolService.GetTxAsync(txId) != null)
            {
                return new TransactionResult
                {
                    TransactionId = txId,
                    Status = Status.Pending
                };
            }
            
            return new TransactionResult
            {
                TransactionId = txId,
                Status = Status.NotExisted
            };
        }

        public async Task AddResultAsync(TransactionResult res)
        {
            _cacheResults[res.TransactionId] = res;
            await _transactionResultManager.AddTransactionResultAsync(res);
        }
    }
}