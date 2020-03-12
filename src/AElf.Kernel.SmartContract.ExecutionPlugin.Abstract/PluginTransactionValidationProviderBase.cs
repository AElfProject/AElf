using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPlugin.Abstract
{
    public abstract class PluginTransactionValidationProviderBase : ITransactionValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        protected PluginTransactionValidationProviderBase(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var contractAddress =
                _smartContractAddressService.GetAddressByContractName(GetInvolvedSystemContractHashName());

            if (!CheckContractAddress(transaction, contractAddress))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(!CheckContractMethod(transaction, GetInvolvedSmartContractMethods().ToArray()));
        }

        public abstract bool ValidateWhileSyncing { get; }

        protected abstract Hash GetInvolvedSystemContractHashName();

        protected abstract List<string> GetInvolvedSmartContractMethods();

        private bool CheckContractAddress(Transaction transaction, Address contractAddress)
        {
            return transaction.To == contractAddress;
        }

        private bool CheckContractMethod(Transaction transaction, params string[] methodNames)
        {
            return methodNames.Any(methodName => methodName == transaction.MethodName);
        }
    }
}