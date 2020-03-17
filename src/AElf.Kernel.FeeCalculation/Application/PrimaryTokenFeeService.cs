using System.Threading.Tasks;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class PrimaryTokenFeeService : IPrimaryTokenFeeService
    {
        private readonly IPrimaryTokenFeeProvider _primaryTokenFeeProvider;

        public PrimaryTokenFeeService(IPrimaryTokenFeeProvider primaryTokenFeeProvider)
        {
            _primaryTokenFeeProvider = primaryTokenFeeProvider;
        }

        public async Task<long> CalculateFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext)
        {
            return await _primaryTokenFeeProvider.CalculateFeeAsync(transactionContext, chainContext);
        }
    }
}