using System.Collections.Generic;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
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
            return new List<Address>
            {
                _smartContractAddressService.GetAddressByContractName(VoteSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(ProfitSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(AssociationSmartContractAddressNameProvider
                    .Name),
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(ParliamentSmartContractAddressNameProvider
                    .Name),
                _smartContractAddressService.GetAddressByContractName(ReferendumSmartContractAddressNameProvider
                    .Name),
                _smartContractAddressService.GetAddressByContractName(TokenConverterSmartContractAddressNameProvider
                    .Name),
            }.Contains(transaction.From);
        }
    }
}