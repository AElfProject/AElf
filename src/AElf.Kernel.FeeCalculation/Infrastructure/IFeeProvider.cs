using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface IFeeProvider
    {
        Task<long> CalculateFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);
    }

    public interface IPrimaryTokenFeeProvider : IFeeProvider
    {
    }

    public interface IResourceTokenFeeProvider : IFeeProvider
    {
        /// <summary>
        /// To identify provider for specific resource token symbol.
        /// </summary>
        string TokenName { get; }
    }
}