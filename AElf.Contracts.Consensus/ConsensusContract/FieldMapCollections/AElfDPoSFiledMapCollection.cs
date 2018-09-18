using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.ConsensusContract.FieldMapCollections
{
    // ReSharper disable InconsistentNaming
    public class AElfDPoSFiledMapCollection
    {
        /// <summary>
        /// Current round number.
        /// </summary>
        public UInt64Field CurrentRoundNumberField;

        /// <summary>
        /// Current block producers / miners.
        /// </summary>
        public PbField<Miners> BlockProducerField;
        
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
        public Map<UInt64Value, Hash> RoundHashMap;
    }
}