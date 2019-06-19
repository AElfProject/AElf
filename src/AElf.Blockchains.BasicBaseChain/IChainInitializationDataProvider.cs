using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;

namespace AElf.Blockchains.BasicBaseChain
{
    public interface IChainInitializationDataProvider
    {
        ConsensusOptions ConsensusOptions { get; }
        ContractOptions ContractOptions { get; }
        
        TokenInitialOptions TokenInitialOptions { get; }
    }
}