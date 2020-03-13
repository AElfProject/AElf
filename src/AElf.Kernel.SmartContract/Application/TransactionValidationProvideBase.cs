using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public abstract class TransactionValidationProvideBase : ITransactionValidationProvider
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

    // public abstract class TokenContractPluginTransactionValidationProviderBase : PluginTransactionValidationProviderBase
    // {
    //     private readonly ISmartContractAddressService _smartContractAddressService;
    //
    //     protected TokenContractPluginTransactionValidationProviderBase(
    //         ISmartContractAddressService smartContractAddressService)
    //     {
    //         _smartContractAddressService = smartContractAddressService;
    //     }
    //
    //     protected override Address InvolvedSystemContractAddress =>
    //         _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
    // }
}