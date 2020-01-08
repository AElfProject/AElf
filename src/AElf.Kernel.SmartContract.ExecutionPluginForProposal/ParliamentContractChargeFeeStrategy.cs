using AElf.Contracts.Parliament;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1.FreeFeeTransactions;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    public class ParliamentContractChargeFeeStrategy : IChargeFeeStrategy
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ParliamentContractChargeFeeStrategy(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Address ContractAddress =>
            _smartContractAddressService.GetAddressByContractName(ParliamentSmartContractAddressNameProvider.Name);

        public string MethodName =>
            nameof(ParliamentContractContainer.ParliamentContractStub.ApproveMultiProposals);
        
        public bool IsFree(Transaction transaction)
        {
            return true;
        }
    }
}