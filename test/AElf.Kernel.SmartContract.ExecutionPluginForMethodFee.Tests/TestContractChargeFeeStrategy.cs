using System.Collections.Generic;
using Acs1;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class TestContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TestContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address ContractAddress =>
            _smartContractAddressService.GetAddressByContractName(HashHelper.ComputeFrom("TestContract"));

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