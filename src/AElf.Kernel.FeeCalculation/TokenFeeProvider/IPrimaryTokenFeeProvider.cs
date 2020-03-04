using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation
{
    public interface IPrimaryTokenFeeProvider
    {
        Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);
    }
}