using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class ConstrainedAEDPoSTransactionValidationProvider : IConstrainedTransactionValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISystemTransactionMethodNameListProvider _coreTransactionMethodNameListProvider;

        public ILogger<ConstrainedAEDPoSTransactionValidationProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<Hash, Transaction> _alreadyHas =
            new ConcurrentDictionary<Hash, Transaction>();

        public ConstrainedAEDPoSTransactionValidationProvider(ISmartContractAddressService smartContractAddressService,
            ISystemTransactionMethodNameListProvider coreTransactionMethodNameListProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _coreTransactionMethodNameListProvider = coreTransactionMethodNameListProvider;
        }

        public bool ValidateTransaction(Transaction transaction, Hash blockHash)
        {
            var consensusContractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            var constrainedTransaction = new Lazy<List<string>>(() =>
                _coreTransactionMethodNameListProvider.GetSystemTransactionMethodNameList());
            if (transaction.To == consensusContractAddress &&
                constrainedTransaction.Value.Contains(transaction.MethodName))
            {
                if (!_alreadyHas.ContainsKey(blockHash))
                {
                    _alreadyHas.TryAdd(blockHash, transaction);
                    return true;
                }

                if (_alreadyHas[blockHash].GetHash() == transaction.GetHash())
                {
                    // Validate twice.
                    return true;
                }

                _alreadyHas.TryRemove(blockHash, out var oldTransaction);
                Logger.LogError(
                    $"Only allow one AEDPoS Contract core transaction.\nNew tx: {transaction}\nOld tx: {oldTransaction}");
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