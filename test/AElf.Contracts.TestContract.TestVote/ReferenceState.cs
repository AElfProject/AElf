using AElf.Contracts.Election;

namespace AElf.Contracts.TestContract.TestVote;

public partial class State
{
    internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
}