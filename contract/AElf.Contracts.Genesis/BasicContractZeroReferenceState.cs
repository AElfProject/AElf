using AElf.Contracts.ParliamentAuth;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZeroState
    {
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
    }
}