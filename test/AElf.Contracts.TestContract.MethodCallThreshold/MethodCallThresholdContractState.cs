using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS5;

namespace AElf.Contracts.TestContract.MethodCallThreshold;

public partial class MethodCallThresholdContractState : ContractState
{
    public MappedState<string, MethodCallingThreshold> MethodCallingThresholds { get; set; }
}