using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class ConstrainedAEDPoSTransactionValidationProvider : ITransactionValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ConstrainedAEDPoSTransactionValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var consensusContractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            var constrainedTransaction = new Lazy<List<string>>(() =>
                new List<string>
                {
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation)
                });
            if (transaction.To == consensusContractAddress &&
                constrainedTransaction.Value.Contains(transaction.MethodName))
            {
                return false;
            }

            return true;
        }
    }
}