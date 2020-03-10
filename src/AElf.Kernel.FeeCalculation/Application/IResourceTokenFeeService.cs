using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Application
{
    public interface IResourceTokenFeeService
    {
        Task<Dictionary<string, long>> CalculateTokenFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext);
    }
}