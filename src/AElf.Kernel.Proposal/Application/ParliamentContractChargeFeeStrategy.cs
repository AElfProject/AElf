using AElf.Contracts.Parliament;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Proposal.Application
{
    // public class ParliamentContractChargeFeeStrategy : IChargeFeeStrategy
    public class ParliamentContractChargeFeeStrategy 
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