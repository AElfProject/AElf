using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Services
{
    public interface IExecutingService
    {
        Task<List<TransactionTrace>> ExecuteAsync(List<Transaction> transactions, int chainId, DateTime currentBlockTime, CancellationToken cancellationToken, Hash disambiguationHash = null, TransactionType transactionType = TransactionType.ContractTransaction, bool skipFee=false);
    }
}