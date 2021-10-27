using AElf.Standards.ACS1;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.BasicSecurity
{
    public partial class BasicSecurityContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public BoolState BoolInfo { get; set; }
        public Int32State Int32Info { get; set; }
        public UInt32State UInt32Info { get; set; }
        public Int64State Int64Info { get; set; }
        public UInt64State UInt64Info { get; set; }
        public StringState StringInfo { get; set; }
        public BytesState BytesInfo { get; set; }
        
        public SingletonState<int> Int32SingletonState { get; set; }
        public SingletonState<StateEnum> EnumState { get; set; }

        public SingletonState<byte[]> BytesSingletonState { get; set; }
        public SingletonState<ProtobufMessage> ProtoInfo { get; set; }
        
        public SingletonState<int>LoopInt32Value { get; set; }
        public ProtobufState<ProtobufMessage> ProtoInfo2 { get; set; }
        
        public MappedState<long, ProtobufMessage> MappedState { get; set; }
        public MappedState<long, string, ProtobufMessage> Complex3Info { get; set; }
        
        public MappedState<long, string, string, ProtobufMessage> Complex4Info { get; set; }
        public MappedState<string, string, string, string, TradeMessage> Complex5Info { get; set; }
        
        public MappedState<string, MethodFees> TransactionFees { get; set; }
    }
}