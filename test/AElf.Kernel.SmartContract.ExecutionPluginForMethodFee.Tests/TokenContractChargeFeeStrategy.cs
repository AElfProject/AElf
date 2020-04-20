using System.Collections.Generic;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class TokenContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TokenContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address GetContractAddress(IChainContext chainContext)
        {
            return AsyncHelper.RunSync(() =>
                _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                    TokenSmartContractAddressNameProvider.StringName));
        }

        public string MethodName => string.Empty;

        public bool IsFree(Transaction transaction)
        {
            return new List<string>
            {
                nameof(TokenContractContainer.TokenContractStub.Create),
                nameof(TokenContractContainer.TokenContractStub.Issue),
            }.Contains(transaction.MethodName);
        }
    }
}