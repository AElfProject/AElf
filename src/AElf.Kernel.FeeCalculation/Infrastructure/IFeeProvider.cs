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
        int[] PieceTypeArray { get; set; }

        Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);
        void UpdatePieceWiseFunction(int[] pieceTypeArray);
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