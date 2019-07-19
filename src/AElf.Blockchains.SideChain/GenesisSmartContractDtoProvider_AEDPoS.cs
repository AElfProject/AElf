using System.Linq;
using Acs0;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.SideChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateConsensusInitializationCallList(ChainInitializationData chainInitializationData)
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();

            var miners = chainInitializationData == null
                ? new MinerList
                {
                    Pubkeys =
                    {
                        _consensusOptions.InitialMiners.Select(p => p.ToByteString())
                    }
                }
                : MinerListWithRoundNumber.Parser.ParseFrom(chainInitializationData.ExtraInformation[0]).MinerList;
            var timestamp = chainInitializationData?.CreationTimestamp ?? _consensusOptions.StartTimestamp;
            consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    IsSideChain = true
                });
            consensusMethodCallList.Add(nameof(AEDPoSContractContainer.AEDPoSContractStub.FirstRound),
                miners.GenerateFirstRoundOfNewTerm(_consensusOptions.MiningInterval, timestamp));
            return consensusMethodCallList;
        }
    }
}