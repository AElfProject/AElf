using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public class BasicTransactionValidationForTxHubProvider : ITransactionValidationForTxHubProvider
    {
        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            return Task.FromResult(true);
        }
    }
}