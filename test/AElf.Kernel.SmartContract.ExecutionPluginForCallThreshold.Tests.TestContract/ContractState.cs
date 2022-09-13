using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;
using AElf.Standards.ACS5;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold.Tests.TestContract;

public class ContractState : Sdk.CSharp.State.ContractState
{
    public MappedState<string, MethodCallingThreshold> MethodCallingThresholdFees { get; set; }
    internal ACS0Container.ACS0ReferenceState Acs0Contract { get; set; }
}