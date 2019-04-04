using System.Linq;
using AElf.Consensus.DPoS;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        public override Empty UpdateMainChainConsensus(ConsensusInformation input)
        {
            // TODO: Only cross chain contract can call UpdateMainChainConsensus method of consensus contract.
            
            // For now we just extract the miner list from main chain consensus information, then update miners list.
            if(input == null || input.Bytes.IsEmpty)
                return new Empty();
            var consensusInformation = DPoSHeaderInformation.Parser.ParseFrom(input.Bytes);
            
            if(consensusInformation.Round.TermNumber <= State.TermNumberFromMainChainField.Value)
                return new Empty();
            Context.LogDebug(() => $"Shared BP of term {consensusInformation.Round.TermNumber.ToInt64Value()}");
            var minersKeys = consensusInformation.Round.RealTimeMinersInformation.Keys;
            State.TermNumberFromMainChainField.Value = consensusInformation.Round.TermNumber;
            State.CurrentMiners.Value = minersKeys.ToList().ToMiners();
            return new Empty();
        }
    }
}