using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS0;

namespace AElf.Contracts.TestContract.ResourceSpender;

public class ResourceSpenderContractState : ContractState
{
    internal ACS0Container.ACS0ReferenceState ACS0Contract { get; set; }
}