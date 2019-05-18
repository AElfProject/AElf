using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Sdk.CSharp;
using Google.Protobuf;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForConsensus(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract<AEDPoSContract>(ConsensusSmartContractAddressNameProvider.Name,
                GenerateConsensusInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList()
        {
            var aelfConsensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            aelfConsensusMethodCallList.Add(nameof(AEDPoSContract.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    TimeEachTerm = _consensusOptions.TimeEachTerm
                });
            aelfConsensusMethodCallList.Add(nameof(AEDPoSContract.FirstRound),
                new MinerList
                {
                    PublicKeys =
                    {
                        _consensusOptions.InitialMiners.Select(p => p.ToByteString())
                    }
                }.GenerateFirstRoundOfNewTerm(_consensusOptions.MiningInterval,
                    _consensusOptions.StartTimestamp.ToUniversalTime()));
            return aelfConsensusMethodCallList;
        }
    }
}