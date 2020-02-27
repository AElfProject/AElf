using System;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.FeeCalculation
{
    public interface IResourceTokenFeeProvider
    {
       string TokenName { get; }
       long CalculateTokenFeeAsync(TransactionContext transactionContext);
    }
}