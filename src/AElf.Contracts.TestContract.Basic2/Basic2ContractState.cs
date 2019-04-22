using AElf.Contracts.TestContract.Basic1;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.Basic2
{
    public partial class Basic2ContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        
        public BoolState BoolInfo { get; set; }
        public Int32State Int32Info { get; set; }
        public UInt32State UInt32Info { get; set; }
        public Int64State Int64Info { get; set; }
        public UInt64State UInt64Info { get; set; }
        public StringState StringInfo { get; set; }
        public BytesState BytesInfo { get; set; }
        public ProtobufState<ProtobufMessage> ProtoInfo2 { get; set; }
        
        public MappedState<long, string, ProtobufMessage> Complex3Info { get; set; }
        public MappedState<string, string, string, string, TradeMessage> Complex4Info { get; set; }
        
        //reference contract
        internal Basic1ContractContainer.Basic1ContractReferenceState Basic1TestContract { get; set; }
    }
}