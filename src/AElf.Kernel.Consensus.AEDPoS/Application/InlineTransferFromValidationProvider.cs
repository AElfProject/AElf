using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
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
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(ElectionSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(EconomicSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(TreasurySmartContractAddressNameProvider.Name)
            }.Contains(transaction.From);
        }
    }
}