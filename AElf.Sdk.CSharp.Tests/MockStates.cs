using AElf.Sdk.CSharp.State;
using AElf.Common;

namespace AElf.Sdk.CSharp.Tests
{
    public class MockStructuredState : StructuredState
    {
        public StringState StringState { get; set; }
    }

    public class MockContractState : ContractState
    {
        public BoolState BoolState { get; set; }
        public Int32State Int32State { get; set; }
        public UInt32State UInt32State { get; set; }
        public Int64State Int64State { get; set; }
        public UInt64State UInt64State { get; set; }
        public StringState StringState { get; set; }
        public BytesState BytesState { get; set; }
        public MockStructuredState StructuredState { get; set; }
        public MappedState<Address, Address, string> MappedState { get; set; }
    }
}