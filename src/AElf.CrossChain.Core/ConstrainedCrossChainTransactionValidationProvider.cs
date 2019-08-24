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
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<ConstrainedCrossChainTransactionValidationProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<Hash, string> _alreadyHas = new ConcurrentDictionary<Hash, string>();

        public ConstrainedCrossChainTransactionValidationProvider(
            ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public bool ValidateTransaction(Transaction transaction, Hash blockHash)
        {
            var crossChainContractAddress =
                _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
            var constrainedTransaction = new Lazy<List<string>>(() =>
                new List<string>
                {
                    nameof(CrossChainContractContainer.CrossChainContractStub.RecordCrossChainData),
                });
            if (transaction.To == crossChainContractAddress &&
                constrainedTransaction.Value.Contains(transaction.MethodName))
            {
                if (!_alreadyHas.ContainsKey(blockHash))
                {
                    _alreadyHas.TryAdd(blockHash, transaction.MethodName);
                    return true;
                }

                _alreadyHas.TryRemove(blockHash, out _);
                Logger.LogError($"Only allow one Cross Chain Contract core transaction '{transaction.MethodName}'");
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