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
            return new List<Address>
            {
                _smartContractAddressService.GetAddressByContractName(VoteSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(ProfitSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(AssociationAuthSmartContractAddressNameProvider
                    .Name),
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(ParliamentAuthSmartContractAddressNameProvider
                    .Name),
                _smartContractAddressService.GetAddressByContractName(ReferendumAuthSmartContractAddressNameProvider
                    .Name),
                _smartContractAddressService.GetAddressByContractName(TokenConverterSmartContractAddressNameProvider
                    .Name),
            }.Contains(transaction.From);
        }
    }
}