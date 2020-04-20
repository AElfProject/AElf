using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Kernel.FeeCalculation.Application
{
    public abstract class TokenContractTransactionValidationProviderBase : TransactionContractInfoValidationProviderBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        protected TokenContractTransactionValidationProviderBase(
            ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        protected override Task<Address> GetInvolvedSystemContractAddressAsync(IChainContext chainContext)
        {
            return _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);
        }
    }
}