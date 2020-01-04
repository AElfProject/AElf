using Acs1;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.Performance
{
    public class PerformanceContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        
        public StringState ContractName { get; set; }
        
        public ProtobufState<Address> ContractManager { get; set; }
        
        public MappedState<Address, string> Content { get; set; }
        
        public MappedState<Address, long, long> MapContent { get; set; }
        
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
    }
}