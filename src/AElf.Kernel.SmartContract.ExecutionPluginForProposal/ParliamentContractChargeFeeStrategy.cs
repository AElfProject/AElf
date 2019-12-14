using AElf.Contracts.ParliamentAuth;
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
            _smartContractAddressService.GetAddressByContractName(ParliamentAuthSmartContractAddressNameProvider.Name);

        public string MethodName =>
            nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.ApproveMultiProposals);
        
        public bool IsFree(Transaction transaction)
        {
            return true;
        }
    }
}