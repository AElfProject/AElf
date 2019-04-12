using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContractExecution.Domain
{
    public interface ITransactionTraceManager
    {
        Task AddTransactionTraceAsync(TransactionTrace tr, Hash disambiguationHash = null);

        Task<TransactionTrace> GetTransactionTraceAsync(Hash txId, Hash disambiguationHash = null);
    }
}