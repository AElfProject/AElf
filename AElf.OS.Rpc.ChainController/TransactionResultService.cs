using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;

namespace AElf.OS.Rpc.ChainController
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
            var receipt = await _txHub.GetTransactionReceiptAsync(txId);
            if (receipt != null)
            {
                return new TransactionResult
                {
                    TransactionId = receipt.TransactionId,
                    Status = TransactionResultStatus.Pending
                };
            }

            // not existed
            return new TransactionResult
            {
                TransactionId = txId,
                Status = TransactionResultStatus.NotExisted
            };
        }

        /// <inheritdoc/>
        public async Task AddResultAsync(TransactionResult res)
        {
            await _transactionResultManager.AddTransactionResultAsync(res);
        }
    }
}