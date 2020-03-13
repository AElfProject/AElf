using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Application
{
    public interface IPrimaryTokenFeeService
    {
        Task<long> CalculateFeeAsync(ITransactionContext transactionContext, IChainContext chainContext);
    }
}