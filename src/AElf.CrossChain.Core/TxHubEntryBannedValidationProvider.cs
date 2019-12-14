using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.CrossChain
{
    public class TxHubEntryBannedValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public TxHubEntryBannedValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var crossChainContractAddress =
                _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);

            return Task.FromResult(transaction.To != crossChainContractAddress ||
                                   CrossChainContractPrivilegeMethodNameProvider.PrivilegeMethodNames.All(methodName =>
                                       methodName != transaction.MethodName));
        }
    }
}