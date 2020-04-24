using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.CrossChain.Application
{
    public class TxHubEntryPermissionValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction,IChainContext chainContext)
        {
            var crossChainContractAddress = await _smartContractAddressService.GetAddressByContractNameAsync(
                chainContext, CrossChainSmartContractAddressNameProvider.StringName);

            return transaction.To != crossChainContractAddress ||
                   CrossChainContractPrivilegeMethodNameProvider.PrivilegeMethodNames.All(methodName =>
                       methodName != transaction.MethodName);
        }
    }
}