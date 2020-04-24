using System.Collections.Generic;
using AElf.Contracts.Genesis;
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
                nameof(BasicContractZeroContainer.BasicContractZeroStub.DeploySmartContract),
                nameof(BasicContractZeroContainer.BasicContractZeroStub.DeploySystemSmartContract),
            }.Contains(transaction.MethodName);
        }
    }
}