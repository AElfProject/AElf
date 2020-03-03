using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.FeeCalculation
{
    public interface IPrimaryTokenFeeProvider
    {
        Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);
    }
}