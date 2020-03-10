using AElf.Contracts.Parliament;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Proposal.Application
{
    public class ProposalTransactionRecognizer : SystemTransactionRecognizerBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ProposalTransactionRecognizer(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public override bool IsSystemTransaction(Transaction transaction)
        {
            return CheckSystemContractAddress(transaction.To, _smartContractAddressService.GetAddressByContractName(
                       ParliamentSmartContractAddressNameProvider.Name)) &&
                   CheckSystemContractMethod(transaction.MethodName,
                       nameof(ParliamentContractContainer.ParliamentContractStub.ApproveMultiProposals));
        }
    }
}