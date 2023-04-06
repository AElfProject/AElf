using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Runtime.CSharp.Tests.TestContract;

public class TestContractState : ContractState
{
    public BoolState BoolInfo { get; set; }
    public Int32State Int32Info { get; set; }
    public UInt32State UInt32Info { get; set; }
    public Int64State Int64Info { get; set; }
    public UInt64State UInt64Info { get; set; }
    public StringState StringInfo { get; set; }
    public BytesState BytesInfo { get; set; }
    public ProtobufState<ProtobufMessage> ProtoInfo { get; set; }

    public MappedState<long, string, ProtobufMessage> Complex3Info { get; set; }
    public MappedState<string, string, string, string, TradeMessage> Complex4Info { get; set; }

    public ReadonlyState<bool> ReadonlyBool { get; set; }

    public MappedState<long, Address> MappedState { get; set; }

    public MappedState<long, long> MappedInt64State { get; set; }
}