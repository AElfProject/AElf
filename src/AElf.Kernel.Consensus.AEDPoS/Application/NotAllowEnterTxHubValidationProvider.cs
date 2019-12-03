using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class NotAllowEnterTxHubValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public NotAllowEnterTxHubValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var consensusContractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            if (transaction.To != consensusContractAddress)
            {
                return true;
            }
            
            var economicContractAddress = 
                _smartContractAddressService.GetAddressByContractName(EconomicSmartContractAddressNameProvider.Name);
            if (transaction.To == economicContractAddress)
            {
                return false;
            }

            var systemTxs = new List<string>
            {
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateValue),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateTinyBlockInformation),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.NextTerm),
                nameof(AEDPoSContractContainer.AEDPoSContractStub.UpdateConsensusInformation),
            };
            return !systemTxs.Contains(transaction.MethodName);
        }
    }
}