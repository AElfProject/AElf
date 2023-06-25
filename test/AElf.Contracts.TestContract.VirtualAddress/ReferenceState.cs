using AElf.Contracts.Election;

namespace AElf.Contracts.TestContract.VirtualAddress;

public partial class State
{
    internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
}