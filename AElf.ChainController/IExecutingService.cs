using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.SmartContract;
using AElf.Common;

namespace AElf.ChainController
{
    public interface IExecutingService
    {
        Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, Hash chainId, CancellationToken cancellationToken, Hash disambiguationHash=null);
    }
}