using AElf.Contracts.CrossChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using AElf.Types;

namespace AElf.CrossChain
{
    public class CrossChainContractFeeChargeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public CrossChainContractFeeChargeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address ContractAddress =>
            _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);

        public string MethodName => nameof(CrossChainContractContainer.CrossChainContractStub.RecordCrossChainData);

        public bool IsFree(Transaction transaction)
        {
            return true;
        }
    }
}