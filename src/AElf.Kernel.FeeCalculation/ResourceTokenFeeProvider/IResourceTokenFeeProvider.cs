using System;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.FeeCalculation
{
    public interface IResourceTokenFeeProvider
    {
        long CalculateTokenFeeAsync(TransactionContext transactionContext);
    }
}