using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.DPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForConsensus(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract<ConsensusContract>(ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList()
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialDPoSContract),
                new InitialDPoSContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    DividendsContractSystemName = DividendSmartContractAddressNameProvider.Name,
                    LockTokenForElection = _tokenInitialOptions.LockForElection
                });
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                _dposOptions.InitialMiners.ToMiners().GenerateFirstRoundOfNewTerm(_dposOptions.MiningInterval,
                    _dposOptions.StartTimestamp.ToUniversalTime()));
            consensusMethodCallList.Add(nameof(ConsensusContract.ConfigStrategy),
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