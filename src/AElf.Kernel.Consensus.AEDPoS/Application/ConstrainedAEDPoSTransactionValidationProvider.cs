using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class ConstrainedAEDPoSTransactionValidationProvider : IConstrainedTransactionValidationProvider
    {
        private readonly Address _consensusContractAddress;

        public ILogger<ConstrainedAEDPoSTransactionValidationProvider> Logger { get; set; }

        private readonly ConcurrentDictionary<Hash, Transaction> _alreadyHas =
            new ConcurrentDictionary<Hash, Transaction>();

        public ConstrainedAEDPoSTransactionValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _consensusContractAddress =
                smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);

            Logger = NullLogger<ConstrainedAEDPoSTransactionValidationProvider>.Instance;
        }

        public bool ValidateTransaction(Transaction transaction, Hash blockHash)
        {
            if (transaction.To != _consensusContractAddress || !new List<string>
            {
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm)
            }.Contains(transaction.MethodName)) return true;

            if (!_alreadyHas.ContainsKey(blockHash))
            {
                _alreadyHas.TryAdd(blockHash, transaction);
                return true;
            }

            if (_alreadyHas[blockHash].GetHash() == transaction.GetHash())
            {
                // Validate twice or more.
                return true;
            }

            _alreadyHas.TryRemove(blockHash, out var oldTransaction);
            Logger.LogWarning(
                $"Only allow one AEDPoS Contract core transaction. New tx: {transaction}, Old tx: {oldTransaction}");
            return false;

        }
    }
}