using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface IFeeProvider
    {
        Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);
    }

    public interface IPrimaryTokenFeeProvider : IFeeProvider
    {
    }

    public interface IResourceTokenFeeProvider : IFeeProvider
    {
        string TokenName { get; }
    }
}