using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
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
            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            if (transaction.To != tokenContractAddress || transaction.MethodName != "TransferFrom") return true;
            return transaction.From ==
                   _smartContractAddressService.GetAddressByContractName(
                       CrossChainSmartContractAddressNameProvider.Name);
        }
    }
}