using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Kernel.FeeCalculation.Application
{
    public abstract class TokenContractTransactionValidationProviderBase : TransactionValidationProvideBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        protected TokenContractTransactionValidationProviderBase(
            ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        protected override Address InvolvedSystemContractAddress =>
            _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
    }
}