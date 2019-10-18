using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
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
            var constrainedTransaction = new Lazy<List<string>>(() =>
                new List<string>
                {
                    nameof(CrossChainContractContainer.CrossChainContractStub.RecordCrossChainData),
                });
            if (transaction.To == _crossChainContractAddress &&
                constrainedTransaction.Value.Contains(transaction.MethodName))
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
                Logger.LogError(
                    $"Only allow one Cross Chain Contract core transaction\nNew tx: {transaction}\nOld tx: {oldTransaction}");
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