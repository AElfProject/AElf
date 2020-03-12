using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface IFeeProvider
    {
        /// <summary>
        /// 0 - Liner function
        /// 1 - Power function
        /// </summary>

        Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);

        void UpdatePieceWiseFunction(List<int[]> pieceTypeList);
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