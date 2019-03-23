using AElf.Common;
using AElf.Sdk.CSharp.State;

namespace AElf.Runtime.CSharp.Tests.TestContract
{
    public class TestContractState : ContractState
    {
        public Int32State Counter { get; set; }
    }
}