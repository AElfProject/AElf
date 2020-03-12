using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPlugin.Abstract
{
    public abstract class PluginTransactionValidationProviderBase : ITransactionValidationProvider
    {
        public virtual Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            return !CheckContractAddress(transaction, InvolvedSystemContractAddress)
                ? Task.FromResult(true)
                : Task.FromResult(!CheckContractMethod(transaction, InvolvedSmartContractMethods));
        }

        public abstract bool ValidateWhileSyncing { get; }

        protected abstract Address InvolvedSystemContractAddress { get; }

        protected abstract string[] InvolvedSmartContractMethods { get; }

        private bool CheckContractAddress(Transaction transaction, Address contractAddress)
        {
            return transaction.To == contractAddress;
        }

        private bool CheckContractMethod(Transaction transaction, params string[] methodNames)
        {
            return methodNames.Any(methodName => methodName == transaction.MethodName);
        }
    }

    public abstract class TokenContractPluginTransactionValidationProviderBase : PluginTransactionValidationProviderBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        protected TokenContractPluginTransactionValidationProviderBase(
            ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        protected override Address InvolvedSystemContractAddress =>
            _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
    }
}