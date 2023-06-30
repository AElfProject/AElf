using AElf.Contracts.Election;
using AElf.Contracts.Profit;

namespace AElf.Contracts.TestContract.VirtualAddress;

public partial class State
{
    internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
    internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
}