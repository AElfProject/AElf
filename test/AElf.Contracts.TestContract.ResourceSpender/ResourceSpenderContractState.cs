using Acs0;
using Acs8;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.ResourceSpender
{
    public partial class ResourceSpenderContractState : ContractState
    {
        internal ACS0Container.ACS0ReferenceState ACS0Contract { get; set; }
    }
}