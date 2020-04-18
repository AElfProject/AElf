using System.Linq;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.ContractInitialization
{
    public class MainChainAEDPosContractInitializationProvider : ContractInitializationProviderBase
    {
        private readonly ConsensusOptions _consensusOptions;
        
        protected override Hash ContractName { get; } = ConsensusSmartContractAddressNameProvider.Name;

        protected override string ContractCodeName { get; } = "AElf.Contracts.Consensus.AEDPoS";

        public MainChainAEDPosContractInitializationProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
        }

        protected override SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateInitializationCallList()
        {
            var aelfConsensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            aelfConsensusMethodCallList.Add(
                nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    PeriodSeconds = _consensusOptions.PeriodSeconds,
                    MinerIncreaseInterval = _consensusOptions.MinerIncreaseInterval
                });
            aelfConsensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                new MinerList
                {
                    Pubkeys =
                    {
                        _consensusOptions.InitialMinerList.Select(p => p.ToByteString())
                    }
                }.GenerateFirstRoundOfNewTerm(_consensusOptions.MiningInterval,
                    _consensusOptions.StartTimestamp));
            return aelfConsensusMethodCallList;
        }
    }
}