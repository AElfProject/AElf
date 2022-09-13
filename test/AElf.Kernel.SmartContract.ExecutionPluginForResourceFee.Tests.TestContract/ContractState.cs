using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract;

public class ContractState : Sdk.CSharp.State.ContractState
{
    public MappedState<string, string> Map { get; set; }
    internal ACS0Container.ACS0ReferenceState Acs0Contract { get; set; }
    internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
    internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
}