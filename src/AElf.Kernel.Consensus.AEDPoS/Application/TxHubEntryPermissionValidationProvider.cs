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

        public async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
        {
            var economicContractAddress = 
                await _smartContractAddressService.GetAddressByContractNameAsync(chainContext, EconomicSmartContractAddressNameProvider.StringName);
            if (transaction.To == economicContractAddress)
            {
                return false;
            }

            var consensusContractAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(chainContext, ConsensusSmartContractAddressNameProvider.StringName);
            if (transaction.To != consensusContractAddress)
            {
                return true;
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