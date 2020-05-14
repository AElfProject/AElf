using System.Collections.Generic;
using Acs0;
using AElf.Kernel;
using AElf.Kernel.FeeCalculation.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Contracts.TestKit
{
    public class ZeroContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ZeroContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address GetContractAddress(IChainContext chainContext)
        {
            return _smartContractAddressService.GetZeroSmartContractAddress();
        }

        public string MethodName => string.Empty;

        public bool IsFree(Transaction transaction)
        {
            return new List<string>
            {
                nameof(ACS0Container.ACS0Stub.DeploySmartContract),
                nameof(ACS0Container.ACS0Stub.DeploySystemSmartContract),
            }.Contains(transaction.MethodName);
        }
    }
}