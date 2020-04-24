using System.Threading.Tasks;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class BasicTransactionValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => true;

        public Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
        {
            if (!transaction.VerifySignature())
                return Task.FromResult(false);

            if (transaction.CalculateSize() > TransactionPoolConsts.TransactionSizeLimit)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}