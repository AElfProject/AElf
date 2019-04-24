using System.Collections.Generic;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS;
using AElf.Kernel;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain.ConsensusModule
{
    public class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly DPoSOptions _dposOptions;
        private readonly TokenInitialOptions _tokenInitialOptions;

        public GenesisSmartContractDtoProvider(DPoSOptions dposOptions,
            TokenInitialOptions tokenInitialOptions)
        {
            _dposOptions = dposOptions;
            _tokenInitialOptions = tokenInitialOptions;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
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