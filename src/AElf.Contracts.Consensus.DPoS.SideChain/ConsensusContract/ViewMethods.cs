using AElf.Consensus.DPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    public partial class ConsensusContract
    {
        public override Round GetCurrentRoundInformation(Empty input)
        {
            return TryToGetRoundNumber(out var roundNumber) ? State.RoundsMap[roundNumber.ToInt64Value()] : null;
        }
    }
}