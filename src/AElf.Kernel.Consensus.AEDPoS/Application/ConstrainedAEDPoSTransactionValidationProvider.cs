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
        private readonly IConsensusCoreTransactionMethodNameListProvider _coreTransactionMethodNameListProvider;

        public ILogger<ConstrainedAEDPoSTransactionValidationProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<Hash, string> _alreadyHas = new ConcurrentDictionary<Hash, string>();

        public ConstrainedAEDPoSTransactionValidationProvider(ISmartContractAddressService smartContractAddressService,
            IConsensusCoreTransactionMethodNameListProvider coreTransactionMethodNameListProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _coreTransactionMethodNameListProvider = coreTransactionMethodNameListProvider;
        }

        public bool ValidateTransaction(Transaction transaction, Hash blockHash)
        {
            var consensusContractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            var constrainedTransaction = new Lazy<List<string>>(() =>
                _coreTransactionMethodNameListProvider.GetCoreTransactionMethodNameList());
            if (transaction.To == consensusContractAddress &&
                constrainedTransaction.Value.Contains(transaction.MethodName))
            {
                if (!_alreadyHas.ContainsKey(blockHash))
                {
                    _alreadyHas.TryAdd(blockHash, transaction.MethodName);
                    return true;
                }

                _alreadyHas.TryRemove(blockHash, out _);
                Logger.LogError($"Only allow one AEDPoS Contract core transaction '{transaction.MethodName}'");
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