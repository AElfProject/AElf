using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class ResourceTokenFeeService : IResourceTokenFeeService
    {
        private readonly IEnumerable<IResourceTokenFeeProvider> _resourceTokenFeeProviders;

        public ResourceTokenFeeService(IEnumerable<IResourceTokenFeeProvider> resourceTokenFeeProviders)
        {
            _resourceTokenFeeProviders = resourceTokenFeeProviders;
        }

        public async Task<Dictionary<string, long>> CalculateFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext)
        {
            var result = new Dictionary<string, long>();
            foreach (var resourceTokenFeeProvider in _resourceTokenFeeProviders)
            {
                result[resourceTokenFeeProvider.TokenName] =
                    await resourceTokenFeeProvider.CalculateFeeAsync(transactionContext,
                        chainContext);
            }

            return result;
        }
    }
}