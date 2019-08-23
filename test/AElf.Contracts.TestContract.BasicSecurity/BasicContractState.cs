using AElf.Contracts.TestContract.BasicFunction;
using AElf.Sdk.CSharp.State;

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
        public ProtobufState<ProtobufMessage> ProtoInfo2 { get; set; }
        
        public MappedState<long, string, ProtobufMessage> Complex3Info { get; set; }
        public MappedState<string, string, string, string, TradeMessage> Complex4Info { get; set; }
        
        //reference contract
        internal BasicFunctionContractContainer.BasicFunctionContractReferenceState BasicFunctionTestContract { get; set; }
    }
}