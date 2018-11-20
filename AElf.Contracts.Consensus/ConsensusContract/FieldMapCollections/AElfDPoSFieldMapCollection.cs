using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.ConsensusContract.FieldMapCollections
{
    // ReSharper disable InconsistentNaming
    public class AElfDPoSFieldMapCollection
    {
        /// <summary>
        /// Current round number.
        /// </summary>
        public UInt64Field CurrentRoundNumberField;

        /// <summary>
        /// Current block producers / miners.
        /// </summary>
        public PbField<OngoingMiners> OngoingMinersField;
        
        /// <summary>
        /// DPoS information of each round.
        /// </summary>
        public Map<UInt64Value, Round> DPoSInfoMap;
        
        /// <summary>
        /// Extra block producer of each round.
        /// </summary>
        public Map<UInt64Value, StringValue> EBPMap;
        
        /// <summary>
        /// Time slot of extra block of current round.
        /// </summary>
        public PbField<Timestamp> TimeForProducingExtraBlockField;
        
        /// <summary>
        /// First block producer of each round, used for generating DPoS information of next round.
        /// </summary>
        public Map<UInt64Value, StringValue> FirstPlaceMap;
        
        /// <summary>
        /// DPoS mining interval.
        /// </summary>
        public Int32Field MiningIntervalField;

        /// <summary>
        /// Using a hash value to identify one round.
        /// Basically the hash value is calculated from signatures of all the BPs.
        /// </summary>
        public Map<UInt64Value, Int64Value> RoundHashMap;

        /// <summary>
        /// Balances of each address.
        /// </summary>
        public Map<Address, Tickets> BalanceMap;

        /// <summary>
        /// The nodes wanna join the election for BP.
        /// </summary>
        public PbField<Candidates> CandidatesFiled;
    }
}