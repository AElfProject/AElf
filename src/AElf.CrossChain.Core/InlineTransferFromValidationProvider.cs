using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.CrossChain
{
    public class InlineTransferFromValidationProvider : IInlineTransactionValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public InlineTransferFromValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public bool Validate(Transaction transaction)
        {
            return transaction.From ==
                   _smartContractAddressService.GetAddressByContractName(
                       CrossChainSmartContractAddressNameProvider.Name);
        }
    }
}