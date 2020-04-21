using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class TxHubEntryPermissionValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            // TODO: Move to other projects. Cannot access EconomicSmartContractAddressNameProvider.
//            var economicContractAddress = 
//                _smartContractAddressService.GetAddressByContractName(EconomicSmartContractAddressNameProvider.Name);
//            if (transaction.To == economicContractAddress)
//            {
//                return Task.FromResult(false);
//            }

            var consensusContractAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            if (transaction.To != consensusContractAddress)
            {
                return Task.FromResult(true);
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
            return Task.FromResult(!systemTxs.Contains(transaction.MethodName));
        }
    }
}