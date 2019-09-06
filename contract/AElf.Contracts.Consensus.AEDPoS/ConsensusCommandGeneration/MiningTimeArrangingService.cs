// ReSharper disable once CheckNamespace

using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        public class MiningTimeArrangingService
        {
            public Timestamp ArrangeNormalBlockMiningTime(Round round, string pubkey, Timestamp currentBlockTime)
            {
                
            }
            
            public Timestamp ArrangeExtraBlockMiningTime(Round round, string pubkey, Timestamp currentBlockTime)
            {
                
            }
        }
    }
}