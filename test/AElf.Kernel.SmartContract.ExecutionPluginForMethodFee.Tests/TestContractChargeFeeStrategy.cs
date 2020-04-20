using System.Collections.Generic;
using Acs1;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class TestContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TestContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address GetContractAddress(IChainContext chainContext)
        {
            return AsyncHelper.RunSync(() =>
                _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                    HashHelper.ComputeFromString("TestContract").ToStorageKey()));
        }

        public string MethodName => string.Empty;

        public bool IsFree(Transaction transaction)
        {
            return new List<string>
            {
                nameof(MethodFeeProviderContractContainer.MethodFeeProviderContractStub.SetMethodFee)
            }.Contains(transaction.MethodName);
        }
    }
}