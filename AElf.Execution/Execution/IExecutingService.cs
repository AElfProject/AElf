using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.Execution.Execution
{
    public interface IExecutingService
    {
        Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId, CancellationToken cancellationToken, Hash disambiguationHash = null, TransactionType transactionType = TransactionType.ContractTransaction);
    }
}