using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForProposal
{
    public class TxHubEntryPermissionValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var parliamentContractAddress =
                _smartContractAddressService.GetAddressByContractName(ParliamentSmartContractAddressNameProvider.Name);

            return Task.FromResult(transaction.To != parliamentContractAddress || transaction.MethodName !=
                                   nameof(ParliamentContractContainer.ParliamentContractStub
                                       .ApproveMultiProposals));
        }
    }
}