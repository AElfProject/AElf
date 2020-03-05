using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface IFeeProvider
    {
        int[] PieceTypeArray { get; set; }
        Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);
        void UpdatePieceWiseFunction(int[] pieceTypeArray);
    }

    public interface IPrimaryTokenFeeProvider : IFeeProvider
    {
    }

    public interface IResourceTokenFeeProvider : IFeeProvider
    {
        string TokenName { get; }
    }
}