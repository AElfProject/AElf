using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Application
{
    public abstract class TransactionContractInfoValidationProviderBase : ITransactionValidationProvider
    {
        public ILocalEventBus LocalEventBus { get; set; }

        public TransactionContractInfoValidationProviderBase()
        {
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public virtual async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
        {
            var transactionId = transaction.GetHash();
            var involvedSystemContractAddress = await GetInvolvedSystemContractAddressAsync(chainContext);
            if (!CheckContractAddress(transaction, involvedSystemContractAddress))
            {
                await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
                {
                    TransactionId = transactionId,
                    TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                    Error = "No such smart contract."
                });
                return false;
            }

            if (!CheckContractMethod(transaction, InvolvedSmartContractMethods))
            {
                await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
                {
                    TransactionId = transactionId,
                    TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                    Error = "No such method."
                });
                return false;
            }

            return true;
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