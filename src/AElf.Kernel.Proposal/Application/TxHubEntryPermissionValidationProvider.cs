using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.Proposal.Application
{
    public class TxHubEntryPermissionValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
        {
            var parliamentContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(
                chainContext, ParliamentSmartContractAddressNameProvider.StringName);

            return transaction.To != parliamentContractAddress || transaction.MethodName !=
                   nameof(ParliamentContractContainer.ParliamentContractStub.ApproveMultiProposals);
        }
    }
}