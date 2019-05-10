using System;
using System.Collections.Generic;
using System.Linq;
using Acs0;
using Acs4;
using AElf.Contracts.Consensus.DPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForConsensus(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("Consensus.DPoS")).Value,
                ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList()
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(ConsensusContractContainer.ConsensusContractStub.InitialDPoSContract),
                new InitialDPoSContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    DividendsContractSystemName = DividendSmartContractAddressNameProvider.Name,
                    LockTokenForElection = _tokenInitialOptions.LockForElection
                });
            consensusMethodCallList.Add(nameof(ConsensusContractContainer.ConsensusContractStub.InitialConsensus),
                _dposOptions.InitialMiners.ToMiners().GenerateFirstRoundOfNewTerm(_dposOptions.MiningInterval,
                    _dposOptions.StartTimestamp.ToUniversalTime()));
            consensusMethodCallList.Add(nameof(ConsensusContractContainer.ConsensusContractStub.ConfigStrategy),
                new DPoSStrategyInput
                {
                    IsBlockchainAgeSettable = _dposOptions.IsBlockchainAgeSettable,
                    IsTimeSlotSkippable = _dposOptions.IsTimeSlotSkippable,
                    IsVerbose = _dposOptions.Verbose
                });
            return consensusMethodCallList;
        }
    }
}