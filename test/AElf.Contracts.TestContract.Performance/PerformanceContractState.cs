using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.Performance
{
    public partial class PerformanceContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        
        public StringState ContractName { get; set; }
        
        public ProtobufState<Address> ContractManager { get; set; }
        
        public MappedState<Address, string> Content { get; set; }
        
        public MappedState<Address, long, long> MapContent { get; set; }
    }
}