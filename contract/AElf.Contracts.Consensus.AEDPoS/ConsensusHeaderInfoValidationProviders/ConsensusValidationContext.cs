using System.Collections.Generic;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class ConsensusValidationContext
    {
        public long CurrentTermNumber { get; set; }
        public long CurrentRoundNumber { get; set; }
        public string Pubkey => ExtraData.SenderPubkey.ToHex();
        public Round BaseRound { get; set; }
        public Round ProvidedRound => ExtraData.Round;
        public Dictionary<long, Round> RoundsDict { get; set; }
        public MappedState<long, Round> Rounds { get; set; }
        public LatestProviderToTinyBlocksCount LatestProviderToTinyBlocksCount { get; set; }
        public AElfConsensusHeaderInformation ExtraData { get; set; }
    }
}