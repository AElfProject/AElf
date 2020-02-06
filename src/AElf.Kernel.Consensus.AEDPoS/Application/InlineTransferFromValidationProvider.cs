using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
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
            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            if (transaction.To != tokenContractAddress || transaction.MethodName != "TransferFrom") return true;
            return new List<Address>
            {
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(ElectionSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(EconomicSmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(TreasurySmartContractAddressNameProvider.Name),
                _smartContractAddressService.GetAddressByContractName(TokenHolderSmartContractAddressNameProvider.Name),
            }.Contains(transaction.From);
        }
    }
}