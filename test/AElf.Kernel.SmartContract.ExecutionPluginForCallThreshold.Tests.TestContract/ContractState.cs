using AElf.Standards.ACS0;
using AElf.Standards.ACS5;
using AElf.Sdk.CSharp.State;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold.Tests.TestContract
{
    public class ContractState : AElf.Sdk.CSharp.State.ContractState
    {
        public MappedState<string, MethodCallingThreshold> MethodCallingThresholdFees { get; set; }
        internal ACS0Container.ACS0ReferenceState Acs0Contract { get; set; }
    }
}