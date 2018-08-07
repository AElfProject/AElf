using AElf.Kernel;
using AElf.Sdk.CSharp.Types;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.Contracts.DPoS.ConsensusContract.FieldMapCollections
{
    // ReSharper disable once InconsistentNaming
    public class AElfDPoSFiledMapCollection
    {
        public UInt64Field CurrentRoundNumberField;
        public PbField<BlockProducer> BlockProducerField;
        // ReSharper disable once InconsistentNaming
        public Map<UInt64Value, RoundInfo> DPoSInfoMap;
        // ReSharper disable once InconsistentNaming
        public Map<UInt64Value, StringValue> EBPMap;
        public PbField<Timestamp> TimeForProducingExtraBlockField;
        public Map<UInt64Value, StringValue> FirstPlaceMap;
    }
}