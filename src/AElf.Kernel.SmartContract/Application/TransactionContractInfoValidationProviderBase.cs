using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public abstract class TransactionContractInfoValidationProviderBase : ITransactionValidationProvider
    {
        public virtual async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
        {
            var involvedSystemContractAddress = await GetInvolvedSystemContractAddressAsync(chainContext);
            return !CheckContractAddress(transaction, involvedSystemContractAddress) ||
                   !CheckContractMethod(transaction, InvolvedSmartContractMethods);
        }

        public abstract bool ValidateWhileSyncing { get; }

        protected abstract Task<Address> GetInvolvedSystemContractAddressAsync(IChainContext chainContext);

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
}