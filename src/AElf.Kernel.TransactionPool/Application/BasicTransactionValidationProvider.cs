using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.TransactionPool.Application
{
    public class BasicTransactionValidationProvider : ITransactionValidationProvider
    {
        public Task<bool> ValidateTransactionAsync(Transaction transaction)
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