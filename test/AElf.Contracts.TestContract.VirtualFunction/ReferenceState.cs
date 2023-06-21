using AElf.Contracts.Election;

namespace AElf.Contracts.TestContract.VirtualFunction;

public partial class State
{
    internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
}