using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    public class BasicTransactionValidationProvider : ITransactionValidationProvider
    {
        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            throw new System.NotImplementedException();
        }
    }
}