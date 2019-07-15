using Acs0;
using Acs8;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.Collections;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract
{
    public class ContractState : AElf.Sdk.CSharp.State.ContractState
    {
        public SingletonState<ResourceTokenBuyingPreferences> ResourceTokenBuyingPreferences { get; set; }
        public MappedState<string, string> Map { get; set; }
        internal ACS0Container.ACS0ReferenceState Acs0Contract { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
    }
}