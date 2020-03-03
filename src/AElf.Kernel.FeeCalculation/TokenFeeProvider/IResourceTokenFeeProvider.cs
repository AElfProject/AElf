using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.FeeCalculation
{
    public interface IResourceTokenFeeProvider
    {
        string TokenName { get; }
        Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, ChainContext chainContext);
    }
}