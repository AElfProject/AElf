using AElf.Kernel;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        public void UpdateMainChainConsensus(byte[] consensusInformationBytes)
        {
            // TODO: Only cross chain contract can call UpdateMainChainConsensus method of consensus contract.
            
            // For now we just extract the miner list from main chain consensus information, then update miners list.
            var consensusInformation = DPoSInformation.Parser.ParseFrom(consensusInformationBytes);
            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.CurrentMiners.Value = minersKeys.ToMiners();
        }
    }
}