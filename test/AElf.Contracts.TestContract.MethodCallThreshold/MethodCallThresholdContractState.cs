using Acs5;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public partial class MethodCallThresholdContractState : ContractState
    {
        public MappedState<string, MethodCallingThreshold> MethodCallingThresholds { get; set; }
    }
}