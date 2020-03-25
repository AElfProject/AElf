using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Contract.TestContract
{
    public class TokenContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TokenContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address ContractAddress =>
            _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

        public string MethodName => string.Empty;

        public bool IsFree(Transaction transaction)
        {
            // Stop charging fee from system txs and plugin txs.
            return new List<string>
            {
                // System tx
                nameof(TokenContractContainer.TokenContractStub.Create),
                nameof(TokenContractContainer.TokenContractStub.Issue),
            }.Contains(transaction.MethodName);
        }
    }
}