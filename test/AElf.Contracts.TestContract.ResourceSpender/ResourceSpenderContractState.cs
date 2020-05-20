using AElf.Standards.ACS0;
using AElf.Standards.ACS8;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.ResourceSpender
{
    public partial class ResourceSpenderContractState : ContractState
    {
        internal ACS0Container.ACS0ReferenceState ACS0Contract { get; set; }
    }
}