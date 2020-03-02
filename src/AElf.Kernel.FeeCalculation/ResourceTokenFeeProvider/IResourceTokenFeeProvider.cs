using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.FeeCalculation
{
    public interface IResourceTokenFeeProvider
    {
        Task<long> CalculateTokenFeeAsync(TransactionContext transactionContext, ChainContext chainContext);
    }
}