using System.Collections.Concurrent;
using System.Linq;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain
{
    public class ConstrainedCrossChainTransactionValidationProvider : IConstrainedTransactionValidationProvider
    {
        private readonly Address _crossChainContractAddress;

        public ILogger<ConstrainedCrossChainTransactionValidationProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<Hash, Transaction> _alreadyHas =
            new ConcurrentDictionary<Hash, Transaction>();

        public ConstrainedCrossChainTransactionValidationProvider(
            ISmartContractAddressService smartContractAddressService)
        {
            _crossChainContractAddress =
                smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
        }

        public bool ValidateTransaction(Transaction transaction, Hash blockHash)
        {
            if (transaction.To == _crossChainContractAddress &&
                CrossChainContractPrivilegeMethodNameProvider.PrivilegeMethodNames.Any(methodName =>
                    methodName == transaction.MethodName))
            {
                if (!_alreadyHas.ContainsKey(blockHash))
                {
                    _alreadyHas.TryAdd(blockHash, transaction);
                    return true;
                }

                if (_alreadyHas[blockHash].GetHash() == transaction.GetHash())
                {
                    return true;
                }

                _alreadyHas.TryRemove(blockHash, out var oldTransaction);
                Logger.LogWarning(
                    $"Only allow one Cross Chain Contract core transaction. New tx: {transaction}, Old tx: {oldTransaction}");
                return false;
            }

            return true;
        }

        public void ClearBlockHash(Hash blockHash)
        {
            _alreadyHas.TryRemove(blockHash, out _);
        }
    }
}